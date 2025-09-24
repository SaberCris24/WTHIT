using Microsoft.UI;
using Microsoft.UI.Xaml;
using Windows.UI;

namespace Plantilla.Helpers
{
    /// <summary>
    /// Provides helper methods for customizing the appearance of
    /// the window title bar caption buttons (minimize, maximize, close).
    /// Allows applying system theme colors, foreground, and background.
    /// </summary>
    internal class TitleBarHelper
    {
        /// <summary>
        /// Applies the current system theme (Dark/Light) to the caption buttons.
        /// Uses white for dark theme, black for light theme.
        /// </summary>
        /// <param name="window">The application window.</param>
        /// <returns>The applied foreground color.</returns>
        public static Color ApplySystemThemeToCaptionButtons(Window window)
        {
            if (window.Content is FrameworkElement rootElement)
            {
                // Choose white for dark theme, black for light theme
                Color color = rootElement.ActualTheme == ElementTheme.Dark ? Colors.White : Colors.Black;
                SetCaptionButtonColors(window, color);
                return color;
            }
            return Colors.Black;
        }

        /// <summary>
        /// Sets the foreground colors of caption buttons (default, hover, pressed, inactive).
        /// </summary>
        /// <param name="window">The application window.</param>
        /// <param name="color">The color to apply.</param>
        public static void SetCaptionButtonColors(Window window, Color color)
        {
            var res = Application.Current.Resources;

            // Update resource dictionary (affects XAML styling if used)
            res["WindowCaptionForeground"] = color;

            // Apply color directly to the title bar buttons
            window.AppWindow.TitleBar.ButtonForegroundColor = color;
            window.AppWindow.TitleBar.ButtonHoverForegroundColor = color;
            window.AppWindow.TitleBar.ButtonPressedForegroundColor = color;
            window.AppWindow.TitleBar.ButtonInactiveForegroundColor = color;
        }

        /// <summary>
        /// Sets the background colors of caption buttons (default, hover, pressed, inactive).
        /// </summary>
        /// <param name="window">The application window.</param>
        /// <param name="color">The background color to apply (nullable).</param>
        public static void SetCaptionButtonBackgroundColors(Window window, Color? color)
        {
            var titleBar = window.AppWindow.TitleBar;

            titleBar.ButtonBackgroundColor = color;
            titleBar.ButtonHoverBackgroundColor = color;
            titleBar.ButtonPressedBackgroundColor = color;
            titleBar.ButtonInactiveBackgroundColor = color;
        }

        /// <summary>
        /// Sets the foreground color of the title bar itself (active/inactive state).
        /// </summary>
        /// <param name="window">The application window.</param>
        /// <param name="color">The foreground color (nullable).</param>
        public static void SetForegroundColor(Window window, Color? color)
        {
            window.AppWindow.TitleBar.ForegroundColor = color;
            window.AppWindow.TitleBar.InactiveForegroundColor = color;
        }

        /// <summary>
        /// Sets the background color of the title bar itself (active/inactive state).
        /// </summary>
        /// <param name="window">The application window.</param>
        /// <param name="color">The background color (nullable).</param>
        public static void SetBackgroundColor(Window window, Color? color)
        {
            window.AppWindow.TitleBar.BackgroundColor = color;
            window.AppWindow.TitleBar.InactiveBackgroundColor = color;
        }
    }
}
