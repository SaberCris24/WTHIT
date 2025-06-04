using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Windows.Storage;

namespace Plantilla.Helpers
{
    public static class ThemeHelper
    {
        public static ElementTheme LoadSavedTheme()
        {
            try
            {
                var savedTheme = ApplicationData.Current?.LocalSettings?.Values["AppTheme"] as string;
                return savedTheme switch
                {
                    nameof(ElementTheme.Dark) => ElementTheme.Dark,
                    nameof(ElementTheme.Light) => ElementTheme.Light,
                    _ => ElementTheme.Default
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading theme: {ex.Message}");
                return ElementTheme.Default;
            }
        }

        public static void UpdateCaptionButtonsForTheme(Window window, ElementTheme theme)
        {
            var color = theme == ElementTheme.Dark ? Colors.White : Colors.Black;
            TitleBarHelper.SetCaptionButtonColors(window, color);
        }
    }
}