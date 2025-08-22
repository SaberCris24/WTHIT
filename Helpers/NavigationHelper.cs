using System;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;

namespace Plantilla.Helpers
{
    public class NavigationHelper
    {
        private readonly NavigationView _navView;

        public NavigationHelper(NavigationView navigationView)
        {
            _navView = navigationView;
        }

        public void LoadNavigationViewPosition()
        {
            try
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                var isLeftMode = localSettings.Values["NavViewIsLeftMode"] as bool? ?? false;

                _navView.PaneDisplayMode = isLeftMode ? 
                    NavigationViewPaneDisplayMode.Auto : 
                    NavigationViewPaneDisplayMode.Top;

                if (!isLeftMode)
                {
                    _navView.IsPaneOpen = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading navigation position: {ex.Message}");
                _navView.PaneDisplayMode = NavigationViewPaneDisplayMode.Auto;
            }
        }

        public void SaveNavigationViewPosition(bool isLeftMode)
        {
            try
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values["NavViewIsLeftMode"] = isLeftMode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving navigation position: {ex.Message}");
            }
        }

        public void UpdateNavigationViewMode(bool isLeftMode)
        {
            try
            {
                if (isLeftMode)
                {
                    _navView.PaneDisplayMode = NavigationViewPaneDisplayMode.Auto;
                    _navView.IsPaneOpen = false;
                }
                else
                {
                    _navView.PaneDisplayMode = NavigationViewPaneDisplayMode.Top;
                    _navView.IsPaneOpen = false;
                }

                SaveNavigationViewPosition(isLeftMode);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating navigation mode: {ex.Message}");
            }
        }
    }
}