using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using Plantilla.Pages;
using System;

namespace Plantilla.Helpers
{
    /// <summary>
    /// Provides helper methods to manage and persist the orientation
    /// of the NavigationView (Left/Auto vs Top).
    /// Supports both packaged (MSIX) and unpackaged apps by using
    /// ApplicationData.LocalSettings or an in-memory fallback.
    /// </summary>
    public static class NavigationOrientationHelper
    {
        // Key used in ApplicationData.LocalSettings to store the orientation preference
        private const string IsLeftModeKey = "NavigationIsOnLeftMode";

        // Fallback value used if the app is not packaged (cannot access LocalSettings)
        private static bool _isLeftMode = true;

        /// <summary>
        /// Gets the stored NavigationView orientation.
        /// Defaults to Left mode if no value is found or in case of error.
        /// </summary>
        /// <returns>True if Left/Auto mode, false if Top mode.</returns>
        public static bool IsLeftMode()
        {
            // For unpackaged apps, use in-memory storage
            if (!NativeHelper.IsAppPackaged)
                return _isLeftMode;

            try
            {
                var valueFromSettings = ApplicationData.Current.LocalSettings.Values[IsLeftModeKey];

                // If no setting exists, initialize it with true (Left mode)
                if (valueFromSettings == null)
                {
                    ApplicationData.Current.LocalSettings.Values[IsLeftModeKey] = true;
                    return true;
                }

                return (bool)valueFromSettings;
            }
            catch (Exception ex)
            {
                // Log error and default to Left mode
                System.Diagnostics.Debug.WriteLine($"Error reading navigation mode: {ex.Message}");
                return true;
            }
        }

        /// <summary>
        /// Updates the NavigationView orientation for a specific element
        /// and persists the setting in LocalSettings or memory fallback.
        /// </summary>
        /// <param name="isLeftMode">True for Left/Auto, false for Top.</param>
        /// <param name="element">The UI element associated with the NavigationView.</param>
        public static void IsLeftModeForElement(bool isLeftMode, UIElement element)
        {
            try
            {
                // Apply UI changes immediately
                UpdateNavigationViewForElement(isLeftMode, element);

                // Save preference depending on packaging
                if (NativeHelper.IsAppPackaged)
                {
                    ApplicationData.Current.LocalSettings.Values[IsLeftModeKey] = isLeftMode;
                }
                else
                {
                    _isLeftMode = isLeftMode;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting navigation mode: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies the NavigationView orientation for the given UI element.
        /// </summary>
        /// <param name="isLeftMode">True for Left/Auto, false for Top.</param>
        /// <param name="element">The element used to retrieve the NavigationRootPage.</param>
        public static void UpdateNavigationViewForElement(bool isLeftMode, UIElement element)
        {
            try
            {
                // Get NavigationRootPage associated with the element
                var navigationRoot = NavigationRootPage.GetForElement(element);
                if (navigationRoot?.NavigationView == null) return;

                var navView = navigationRoot.NavigationView;

                if (isLeftMode)
                {
                    // Use Auto → NavigationView chooses compact Left layout
                    navView.PaneDisplayMode = NavigationViewPaneDisplayMode.Auto;
                    Grid.SetRow(navView, 0);
                }
                else
                {
                    // Use Top layout → navigation bar appears at the top
                    navView.PaneDisplayMode = NavigationViewPaneDisplayMode.Top;
                    Grid.SetRow(navView, 1);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating navigation view: {ex.Message}");
            }
        }
    }
}
