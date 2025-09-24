using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Windows.Storage;
using Windows.UI.ViewManagement;

namespace Plantilla.Helpers
{
    /// <summary>
    /// Provides helper methods for managing and persisting the application's theme (Light, Dark, or Default).
    /// Stores the theme in ApplicationData.LocalSettings and also updates the window caption buttons
    /// to match the active theme.
    /// </summary>
    public static class ThemeHelper
    {
        /// <summary>
        /// Loads the saved theme from local settings.
        /// Defaults to ElementTheme.Default if not found or if loading fails.
        /// </summary>
        /// <returns>The saved theme (Dark, Light, or Default).</returns>
        public static ElementTheme LoadSavedTheme()
        {
            try
            {
                var savedTheme = ApplicationData.Current?.LocalSettings?.Values["AppTheme"] as string;

                // Match string value with ElementTheme enum
                return savedTheme switch
                {
                    nameof(ElementTheme.Dark) => ElementTheme.Dark,
                    nameof(ElementTheme.Light) => ElementTheme.Light,
                    _ => ElementTheme.Default
                };
            }
            catch (Exception ex)
            {
                // Log error and return default
                System.Diagnostics.Debug.WriteLine($"Error loading theme: {ex.Message}");
                return ElementTheme.Default;
            }
        }

        /// <summary>
        /// Saves the selected theme to local settings as a string.
        /// </summary>
        /// <param name="theme">The theme to save (Dark, Light, or Default).</param>
        public static void SaveTheme(ElementTheme theme)
        {
            try
            {
                ApplicationData.Current.LocalSettings.Values["AppTheme"] = theme.ToString();
            }
            catch (Exception ex)
            {
                // Log error if saving fails
                System.Diagnostics.Debug.WriteLine($"Error saving theme: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the caption button colors (minimize, maximize, close)
        /// to ensure visibility depending on the active theme.
        /// </summary>
        /// <param name="window">The application window.</param>
        /// <param name="theme">The current theme (Dark or Light).</param>
        public static void UpdateCaptionButtonsForTheme(Window window, ElementTheme theme)
        {
            if (window == null) return;

            // Use white buttons for dark theme, black for light theme
            var color = theme == ElementTheme.Dark ? Colors.White : Colors.Black;
            TitleBarHelper.SetCaptionButtonColors(window, color);
        }

        /// <summary>
        /// Gets the effective theme based on the selected theme and system settings.
        /// If Default is selected, this method determines the system theme by analyzing
        /// the system background color.
        /// </summary>
        /// <param name="theme">The selected theme (Dark, Light, or Default).</param>
        /// <returns>The resolved theme (Dark or Light).</returns>
        public static ElementTheme GetEffectiveTheme(ElementTheme theme)
        {
            if (theme != ElementTheme.Default)
                return theme;

            try
            {
                var uiSettings = new UISettings();
                var background = uiSettings.GetColorValue(UIColorType.Background);

                // Determine brightness: if the sum of RGB values is greater than 384
                // (half of 255*3), consider the background light, otherwise dark.
                return (background.R + background.G + background.B) > 384
                    ? ElementTheme.Light
                    : ElementTheme.Dark;
            }
            catch
            {
                // Fallback to Light theme if system query fails
                return ElementTheme.Light;
            }
        }
    }
}