using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using WTHIT.Core;

namespace WTHIT.Services
{
    /// <summary>Loads and shares process icons across views on the UI thread.</summary>
    public static class ProcessIconService
    {
        private static readonly ExpiringCache<string, BitmapImage> IconCache =
            new(TimeSpan.FromMinutes(10), Configuration.DefaultMaxPoolSize);
        private static readonly Dictionary<string, Task<BitmapImage?>> PendingIcons =
            new(StringComparer.OrdinalIgnoreCase);

        private static Task<BitmapImage?>? _defaultIcon;

        /// <summary>Gets an executable icon or the generic application icon. Call from the UI thread.</summary>
        public static async Task<BitmapImage?> GetIconForProcessAsync(int processId, string? knownPath = null)
        {
            string? path = await Task.Run(() => File.Exists(knownPath)
                ? knownPath
                : ProcessIconExtractor.GetImagePath(processId));

            if (path == null)
                return await GetDefaultIconAsync();

            string key = path.ToUpperInvariant();
            if (IconCache.TryGet(key, out var cachedIcon))
                return cachedIcon;

            if (PendingIcons.TryGetValue(path, out var pendingIcon))
                return await pendingIcon;

            var load = LoadExecutableIconAsync(path, key);
            PendingIcons.Add(path, load);
            try
            {
                return await load;
            }
            finally
            {
                PendingIcons.Remove(path);
            }
        }

        private static async Task<BitmapImage?> LoadExecutableIconAsync(string path, string key)
        {
            var png = await Task.Run(() => ProcessIconExtractor.ExtractPng(path));
            var icon = await DecodeIconAsync(png);
            if (icon != null)
            {
                IconCache.Set(key, icon);
                return icon;
            }

            icon = await GetDefaultIconAsync();
            if (icon != null)
                IconCache.Set(key, icon, TimeSpan.FromMinutes(1));

            return icon;
        }

        private static async Task<BitmapImage?> GetDefaultIconAsync()
        {
            var load = _defaultIcon ??= LoadDefaultIconAsync();
            var icon = await load;
            if (icon == null && ReferenceEquals(_defaultIcon, load))
                _defaultIcon = null;

            return icon;
        }

        private static async Task<BitmapImage?> LoadDefaultIconAsync()
        {
            var png = await Task.Run(ProcessIconExtractor.GetDefaultApplicationIconPng);
            return await DecodeIconAsync(png);
        }

        private static async Task<BitmapImage?> DecodeIconAsync(byte[]? png)
        {
            if (png == null)
                return null;

            try
            {
                using var buffer = new MemoryStream(png);
                using var stream = buffer.AsRandomAccessStream();
                var icon = new BitmapImage { DecodePixelWidth = 64 };
                await icon.SetSourceAsync(stream);
                return icon;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to decode a process icon: {ex.Message}");
                return null;
            }
        }
    }
}
