using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WTHIT.Core;
using System;
using System.Runtime.InteropServices;
using WinRT.Interop;
using Windows.Graphics;

namespace WTHIT.Helpers
{
    /// <summary>
    /// Comprehensive window management helper - optimized
    /// </summary>
    internal static class WindowSetupHelper
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr LoadImage(IntPtr hInst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

        private const int WM_SETICON = 0x0080;
        private const uint IMAGE_ICON = 1;
        private const uint LR_LOADFROMFILE = 0x00000010;

        public static void SetupWindow(Window window, int initialWidth, int initialHeight, int minWidth = 800, int minHeight = 400, string settingsKey = "WindowPosition")
        {
            if (window == null) return;

            try
            {
                var appWindow = window.AppWindow;
                if (appWindow == null)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to get AppWindow");
                    return;
                }

                if (appWindow.Presenter is OverlappedPresenter presenter)
                {
                    presenter.IsResizable = true;
                    presenter.IsMaximizable = true;
                    presenter.IsMinimizable = true;
                    presenter.IsModal = false;
                    var (x, y, width, height, isMaximized) = LoadWindowState(settingsKey, initialWidth, initialHeight);
                    var rect = GetVisibleBounds(new RectInt32(x, y, width, height), initialWidth, initialHeight, minWidth, minHeight);
                    presenter.PreferredMinimumWidth = Math.Min(minWidth, rect.Width);
                    presenter.PreferredMinimumHeight = Math.Min(minHeight, rect.Height);

                    // Set the restored bounds before maximizing so Windows remembers them.
                    appWindow.MoveAndResize(rect);

                    if (isMaximized)
                    {
                        presenter.Maximize();
                    }
                }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting up window: {ex.Message}");
            }
        }

        /// <summary>Fits restored bounds to a connected display or centers them on the primary display.</summary>
        private static RectInt32 GetVisibleBounds(RectInt32 saved, int defaultWidth, int defaultHeight, int minWidth, int minHeight)
        {
            var savedDisplay = DisplayArea.GetFromRect(saved, DisplayAreaFallback.None);
            var display = savedDisplay ?? DisplayArea.Primary;
            var work = display.WorkArea;
            var screen = display.OuterBounds;
            int left = screen.X + work.X;
            int top = screen.Y + work.Y;
            int width = Math.Clamp(savedDisplay == null ? defaultWidth : saved.Width, Math.Min(minWidth, work.Width), work.Width);
            int height = Math.Clamp(savedDisplay == null ? defaultHeight : saved.Height, Math.Min(minHeight, work.Height), work.Height);
            int x = savedDisplay == null ? left + (work.Width - width) / 2 : Math.Clamp(saved.X, left, left + work.Width - width);
            int y = savedDisplay == null ? top + (work.Height - height) / 2 : Math.Clamp(saved.Y, top, top + work.Height - height);
            return new RectInt32(x, y, width, height);
        }

        private static (int X, int Y, int Width, int Height, bool IsMaximized) LoadWindowState(string settingsKey, int defaultWidth, int defaultHeight)
        {
            try
            {
                var settings = SettingsManager.Instance;

                int x = settings.GetValue($"{settingsKey}_X", 100);
                int y = settings.GetValue($"{settingsKey}_Y", 100);
                int width = settings.GetValue($"{settingsKey}_Width", defaultWidth);
                int height = settings.GetValue($"{settingsKey}_Height", defaultHeight);
                bool isMaximized = settings.GetValue($"{settingsKey}_IsMaximized", false);

                // Use a default size when the saved dimensions are invalid.
                if (width <= 0) width = defaultWidth;
                if (height <= 0) height = defaultHeight;

                return (x, y, width, height, isMaximized);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading window state: {ex.Message}");
                return (100, 100, defaultWidth, defaultHeight, false);
            }
        }

        public static void SetupWindowIcon(Window window)
        {
            if (window == null) return;

            try
            {
                var hwnd = WindowNative.GetWindowHandle(window);
                var iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "icon.ico");

                if (!System.IO.File.Exists(iconPath))
                {
                    System.Diagnostics.Debug.WriteLine($"Icon file not found: {iconPath}");
                    return;
                }

                var hIcon = LoadImage(IntPtr.Zero, iconPath, IMAGE_ICON, 0, 0, LR_LOADFROMFILE);
                if (hIcon != IntPtr.Zero)
                {
                    SendMessage(hwnd, WM_SETICON, (IntPtr)1, hIcon);
                    SendMessage(hwnd, WM_SETICON, (IntPtr)0, hIcon);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting window icon: {ex.Message}");
            }
        }

        public static void SaveWindowPosition(Window window, string settingsKey = "WindowPosition")
        {
            if (window?.AppWindow == null) return;

            try
            {
                var appWindow = window.AppWindow;
                if (appWindow.Presenter is OverlappedPresenter { State: OverlappedPresenterState.Minimized })
                    return;

                var settings = SettingsManager.Instance;

                bool isMaximized = appWindow.Presenter is OverlappedPresenter { State: OverlappedPresenterState.Maximized };
                settings.SetValue($"{settingsKey}_IsMaximized", isMaximized);

                // Keep the restored bounds when the window is maximized.
                if (!isMaximized)
                {
                    var position = appWindow.Position;
                    var size = appWindow.Size;
                    settings.SetValue($"{settingsKey}_X", position.X);
                    settings.SetValue($"{settingsKey}_Y", position.Y);
                    settings.SetValue($"{settingsKey}_Width", size.Width);
                    settings.SetValue($"{settingsKey}_Height", size.Height);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving window position: {ex.Message}");
            }
        }

    }
}
