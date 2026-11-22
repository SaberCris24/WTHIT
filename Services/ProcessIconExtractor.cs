using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace WTHIT.Services
{
    /// <summary>
    /// Resolves executable paths and extracts Windows icons as PNG data.
    /// </summary>
    public static class ProcessIconExtractor
    {
        #region Win32 - Icons

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int PrivateExtractIcons(
            string szFileName,
            int nIconIndex,
            int cxIcon,
            int cyIcon,
            IntPtr[] phicon,
            int[] piconid,
            int nIcons,
            int flags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        #endregion

        #region Win32 - Stock Icon

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHSTOCKICONINFO
        {
            public uint cbSize;
            public IntPtr hIcon;
            public int iSysImageIndex;
            public int iIcon;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szPath;
        }

        [DllImport("Shell32.dll", SetLastError = false)]
        private static extern int SHGetStockIconInfo(
            uint siid,
            uint uFlags,
            ref SHSTOCKICONINFO psii);

        private const uint SHGSI_ICON = 0x100;
        private const uint SHGSI_LARGEICON = 0x0;
        private const uint SIID_APPLICATION = 2;

        #endregion

        #region Win32 - Process Path

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(
            uint dwDesiredAccess,
            bool bInheritHandle,
            int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool QueryFullProcessImageName(
            IntPtr hProcess,
            uint dwFlags,
            StringBuilder lpExeName,
            ref uint lpdwSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        #endregion

        private static readonly int[] PreferredSizes = { 64, 48, 32, 128, 256 };

        /// <summary>
        /// Gets the executable path using a process handle with limited query access.
        /// </summary>
        public static string? GetImagePath(int pid)
        {
            if (pid <= 0)
                return null;

            IntPtr h = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
            if (h == IntPtr.Zero)
                return null;

            try
            {
                var sb = new StringBuilder(1024);
                uint size = (uint)sb.Capacity;

                if (QueryFullProcessImageName(h, 0, sb, ref size))
                {
                    var path = sb.ToString();
                    return string.IsNullOrWhiteSpace(path) ? null : path;
                }

                return null;
            }
            finally
            {
                CloseHandle(h);
            }
        }

        /// <summary>
        /// Extracts an icon from an executable and returns PNG bytes.
        /// Safe to call from a background thread.
        /// </summary>
        public static byte[]? ExtractPng(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return null;

            foreach (var size in PreferredSizes)
            {
                var png = TryExtractAt(filePath, size);
                if (png != null)
                    return png;
            }

            return Fallback(filePath);
        }

        /// <summary>
        /// Returns the generic Windows application icon as PNG bytes.
        /// Safe to call from a background thread.
        /// </summary>
        public static byte[]? GetDefaultApplicationIconPng()
        {
            try
            {
                var sii = new SHSTOCKICONINFO
                {
                    cbSize = (uint)Marshal.SizeOf(typeof(SHSTOCKICONINFO))
                };

                int hr = SHGetStockIconInfo(SIID_APPLICATION, SHGSI_ICON | SHGSI_LARGEICON, ref sii);
                if (hr < 0 || sii.hIcon == IntPtr.Zero)
                    return null;

                try
                {
                    using var icon = Icon.FromHandle(sii.hIcon);
                    using var bitmap = icon.ToBitmap();
                    return ToPng(bitmap);
                }
                finally
                {
                    DestroyIcon(sii.hIcon);
                }
            }
            catch
            {
                return null;
            }
        }

        private static byte[]? TryExtractAt(string filePath, int size)
        {
            var handles = new IntPtr[1];
            var ids = new int[1];

            int extracted = PrivateExtractIcons(filePath, 0, size, size, handles, ids, 1, 0);
            if (extracted <= 0 || handles[0] == IntPtr.Zero)
                return null;

            try
            {
                using var icon = Icon.FromHandle(handles[0]);
                using var bitmap = icon.ToBitmap();

                if (bitmap.Width <= 0 || bitmap.Height <= 0)
                    return null;

                return ToPng(bitmap);
            }
            catch
            {
                return null;
            }
            finally
            {
                DestroyIcon(handles[0]);
            }
        }

        private static byte[]? Fallback(string filePath)
        {
            try
            {
                using var icon = Icon.ExtractAssociatedIcon(filePath);
                if (icon == null)
                    return null;

                using var bitmap = icon.ToBitmap();
                return ToPng(bitmap);
            }
            catch
            {
                return null;
            }
        }

        private static byte[]? ToPng(Bitmap bitmap)
        {
            try
            {
                using var png = new MemoryStream();
                bitmap.Save(png, ImageFormat.Png);
                return png.ToArray();
            }
            catch
            {
                return null;
            }
        }
    }
}