using Microsoft.UI;
using Microsoft.UI.Xaml;
using Windows.UI;

namespace Plantilla.Helpers
{
    internal class TitleBarHelper
    {
        public static Color ApplySystemThemeToCaptionButtons(Window window)
        {
            if (window.Content is FrameworkElement rootElement)
            {
                Color color = rootElement.ActualTheme == ElementTheme.Dark ? Colors.White : Colors.Black;
                SetCaptionButtonColors(window, color);
                return color;
            }
            return Colors.Black;
        }

        public static void SetCaptionButtonColors(Window window, Color color)
        {
            var res = Application.Current.Resources;
            res["WindowCaptionForeground"] = color;
            window.AppWindow.TitleBar.ButtonForegroundColor = color;
            window.AppWindow.TitleBar.ButtonHoverForegroundColor = color;
            window.AppWindow.TitleBar.ButtonPressedForegroundColor = color;
            window.AppWindow.TitleBar.ButtonInactiveForegroundColor = color;
        }

        public static void SetCaptionButtonBackgroundColors(Window window, Color? color)
        {
            var titleBar = window.AppWindow.TitleBar;
            titleBar.ButtonBackgroundColor = color;
            titleBar.ButtonHoverBackgroundColor = color;
            titleBar.ButtonPressedBackgroundColor = color;
            titleBar.ButtonInactiveBackgroundColor = color;
        }

        public static void SetForegroundColor(Window window, Color? color)
        {
            window.AppWindow.TitleBar.ForegroundColor = color;
            window.AppWindow.TitleBar.InactiveForegroundColor = color;
        }

        public static void SetBackgroundColor(Window window, Color? color)
        {
            window.AppWindow.TitleBar.BackgroundColor = color;
            window.AppWindow.TitleBar.InactiveBackgroundColor = color;
        }
    }
}