using System;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;

namespace Plantilla.Helpers
{
    /// <summary>
    /// Provides helper methods for saving, loading, and applying
    /// the NavigationView display mode (LeftCompact or Top).
    /// Persists the chosen mode in local app settings.
    /// </summary>
    public class NavigationHelper
    {
        // Reference to the NavigationView instance being managed
        private readonly NavigationView _navView;

        // Key used in ApplicationData.LocalSettings to store the navigation mode
        private const string NavViewSettingKey = "NavViewIsLeftMode";

        /// <summary>
        /// Initializes a new instance of NavigationHelper.
        /// </summary>
        /// <param name="navigationView">The NavigationView to manage.</param>
        /// <exception cref="ArgumentNullException">Thrown if navigationView is null.</exception>
        public NavigationHelper(NavigationView navigationView)
        {
            // Store the navigation view or throw if null
            _navView = navigationView ?? throw new ArgumentNullException(nameof(navigationView));
        }

        /// <summary>
        /// Loads the saved NavigationView mode from local settings
        /// and applies it. Defaults to Top mode if loading fails.
        /// </summary>
        public void LoadNavigationViewPosition()
        {
            try
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                // Retrieve stored value or use false (Top mode) if not found
                var isLeftMode = localSettings.Values[NavViewSettingKey] as bool? ?? false;

                ApplyNavigationMode(isLeftMode);
            }
            catch (Exception ex)
            {
                // Log error and fallback to default mode
                System.Diagnostics.Debug.WriteLine($"Error loading navigation position: {ex.Message}");
                ApplyNavigationMode(false);
            }
        }

        /// <summary>
        /// Saves the current NavigationView mode to local settings.
        /// </summary>
        /// <param name="isLeftMode">True for LeftCompact, false for Top.</param>
        public async void SaveNavigationViewPosition(bool isLeftMode)
        {
            try
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values[NavViewSettingKey] = isLeftMode;

                // Asynchronous placeholder (keeps method async for future expansion)
                await System.Threading.Tasks.Task.CompletedTask;
            }
            catch (Exception ex)
            {
                // Log error if saving fails
                System.Diagnostics.Debug.WriteLine($"Error saving navigation position: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies the given NavigationView mode and persists it in local settings.
        /// </summary>
        /// <param name="isLeftMode">True for LeftCompact, false for Top.</param>
        public void UpdateNavigationViewMode(bool isLeftMode)
        {
            try
            {
                ApplyNavigationMode(isLeftMode);
                SaveNavigationViewPosition(isLeftMode);
            }
            catch (Exception ex)
            {
                // Log error if update fails
                System.Diagnostics.Debug.WriteLine($"Error updating navigation mode: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies the specified NavigationView display mode to the UI.
        /// </summary>
        /// <param name="isLeftMode">True for LeftCompact, false for Top.</param>
        private void ApplyNavigationMode(bool isLeftMode)
        {
            _navView.PaneDisplayMode = isLeftMode
                ? NavigationViewPaneDisplayMode.LeftCompact
                : NavigationViewPaneDisplayMode.Top;

            // Always close the pane after changing mode
            _navView.IsPaneOpen = false;
        }
    }
}
