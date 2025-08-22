using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Runtime.InteropServices;
using WinRT.Interop;

namespace Plantilla.Helpers
{
    public static class WindowSetupHelper
    {
        #region Win32 API

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr LoadImage(IntPtr hInst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

        private const int WM_SETICON = 0x0080;
        private const uint IMAGE_ICON = 1;
        private const uint LR_LOADFROMFILE = 0x00000010;

        #endregion

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