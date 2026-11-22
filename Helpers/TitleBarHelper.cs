using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.UI;

namespace WTHIT.Helpers
{
    /// <summary>
    /// Helper for managing title bar appearance and behavior - optimized
    /// </summary>
    internal static class TitleBarHelper
    {
        public static void SetCaptionButtonColors(Window window, Color color)
        {
            if (window?.AppWindow?.TitleBar is not { } titleBar) return;

            titleBar.ButtonForegroundColor = color;
            titleBar.ButtonHoverForegroundColor = color;
            titleBar.ButtonPressedForegroundColor = color;
            titleBar.ButtonInactiveForegroundColor = Colors.Gray;
        }

        public static void UpdateForWindowActivation(Window window, bool isActive)
        {
            if (window?.AppWindow?.TitleBar is not { } titleBar) return;
            titleBar.ButtonInactiveForegroundColor = isActive ? titleBar.ButtonForegroundColor : Colors.Gray;
        }
    }
}
