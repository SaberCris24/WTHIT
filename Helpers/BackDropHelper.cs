using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;

namespace Plantilla.Helpers
{
    /// <summary>
    /// Helper class for managing window backdrop effects in WinUI 3
    /// Provides support for Mica, MicaAlt, and Acrylic effects
    /// </summary>
    public static class BackdropHelper
    {
        /// <summary>
        /// Available backdrop types supported by the application
        /// </summary>
        public enum BackdropType
        {
            /// <summary>Standard Mica effect</summary>
            Mica,
            /// <summary>Alternative Mica effect with different opacity</summary>
            MicaAlt,
            /// <summary>Acrylic effect for legacy or fallback</summary>
            Acrylic
        }

        /// <summary>
        /// Sets the backdrop effect for a window
        /// </summary>
        /// <param name="window">Target window for the backdrop</param>
        /// <param name="type">Type of backdrop to apply</param>
        /// <returns>True if backdrop was applied successfully</returns>
        public static bool SetBackdrop(Window window, BackdropType type)
        {
            if (window == null)
                return false;

            try
            {
                // Choose and apply the appropriate backdrop
                window.SystemBackdrop = type switch
                {
                    BackdropType.Mica when MicaController.IsSupported() => 
                        new MicaBackdrop { Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.Base },
                    BackdropType.MicaAlt when MicaController.IsSupported() => 
                        new MicaBackdrop { Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.BaseAlt },
                    BackdropType.Acrylic when DesktopAcrylicController.IsSupported() => 
                        new DesktopAcrylicBackdrop(),
                    _ => new MicaBackdrop() // Default to regular Mica
                };

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to set backdrop: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Determines the best available backdrop type for the current system
        /// </summary>
        /// <returns>The most suitable backdrop type</returns>
        public static BackdropType GetBestAvailableBackdrop()
        {
            if (MicaController.IsSupported())
                return BackdropType.Mica;
            if (DesktopAcrylicController.IsSupported())
                return BackdropType.Acrylic;
            return BackdropType.Mica; // Default to Mica
        }
    }
}