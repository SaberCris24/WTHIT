using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Runtime.InteropServices;
using WinRT.Interop;

namespace Plantilla.Helpers
{
    /// <summary>
    /// Helper class for setting up window properties such as icon, size, and title bar
    /// </summary>
    public static class WindowSetupHelper
    {
        #region Win32 API

        /// <summary>
        /// Sends a message to a window or windows
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Loads an icon, cursor, or bitmap
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr LoadImage(IntPtr hInst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

        /// <summary>
        /// Message to set the window icon
        /// </summary>
        private const int WM_SETICON = 0x0080;
        
        /// <summary>
        /// Image type constant for icons
        /// </summary>
        private const uint IMAGE_ICON = 1;
        
        /// <summary>
        /// Load flag to load image from file
        /// </summary>
        private const uint LR_LOADFROMFILE = 0x00000010;

        #endregion

        /// <summary>
        /// Sets up the window icon from an ICO file
        /// </summary>
        /// <param name="window">The window to set the icon for</param>
        public static void SetupWindowIcon(Window window)
        {
            try
            {
                var hwnd = WindowNative.GetWindowHandle(window);
                string iconPath = System.IO.Path.Combine(Environment.CurrentDirectory, "Assets", "icon.ico");
                
                IntPtr hIcon = LoadImage(IntPtr.Zero, iconPath, IMAGE_ICON, 0, 0, LR_LOADFROMFILE);
                
                SendMessage(hwnd, WM_SETICON, (IntPtr)1, hIcon);
                SendMessage(hwnd, WM_SETICON, (IntPtr)0, hIcon);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting window icon: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets up the window size and minimum size constraints
        /// </summary>
        /// <param name="window">The window to set up</param>
        /// <param name="MinWidth">Minimum width of the window</param>
        /// <param name="MinHeight">Minimum height of the window</param>
        public static void SetupWindowSize(Window window, int MinWidth, int MinHeight)
        {
            try
            {
                var presenter = OverlappedPresenter.Create();
                presenter.IsResizable = true;
                presenter.PreferredMinimumHeight = MinHeight;
                presenter.PreferredMinimumWidth = MinWidth;
                presenter.IsMaximizable = true;
                presenter.IsMinimizable = true;

                var hwnd = WindowNative.GetWindowHandle(window);
                var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
                var appWindow = AppWindow.GetFromWindowId(windowId);
                
                appWindow.Resize(new Windows.Graphics.SizeInt32(MinWidth, MinHeight));
                
                window.AppWindow.SetPresenter(presenter);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting up window size: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets up the title bar with custom styling
        /// </summary>
        /// <param name="window">The window to set up the title bar for</param>
        /// <param name="titleBarElement">UI element to use as the custom title bar</param>
        public static void SetupTitleBar(Window window, UIElement titleBarElement)
        {
            try
            {
                window.ExtendsContentIntoTitleBar = true;
                window.SetTitleBar(titleBarElement);

                window.AppWindow.Title = "WTHIT";
                window.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
                window.AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;

                TitleBarHelper.SetBackgroundColor(window, Colors.Transparent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting up title bar: {ex.Message}");
            }
        }
    }
}