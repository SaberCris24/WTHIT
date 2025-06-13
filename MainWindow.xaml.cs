using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Plantilla.Helpers;
using Plantilla.Pages;
using Plantilla.Pages.About;
using Plantilla.Pages.Processes;
using Plantilla.Pages.Settings;
using System;

namespace Plantilla
{
    public sealed partial class MainWindow : Window
    {
        #region Properties
        public NavigationView NavigationViewControl => NavView;
        private NavigationHelper _navigationHelper;
        #endregion

        public MainWindow(int MinWidth, int MinHeight)
        {
            try
            {
                this.InitializeComponent();

                // Initialize NavigationRootPage with the NavigationView
                NavigationRootPage.Initialize(NavView);

                // Initialize navigation helper - must be after InitializeComponent to ensure NavView exists
                _navigationHelper = new NavigationHelper(NavView);

                // Set up window components
                WindowSetupHelper.SetupTitleBar(this, AppTitleBar);
                WindowSetupHelper.SetupWindowSize(this, MinWidth, MinHeight);
                WindowSetupHelper.SetupWindowIcon(this);

                // Setup theme
                SetupTheme();

                // Navigate to default page
                contentFrame.Navigate(typeof(ProcessesPage));

                // Initialize NavigationView position based on saved configuration
                bool isLeftMode = NavigationOrientationHelper.IsLeftMode();
                NavigationOrientationHelper.IsLeftModeForElement(isLeftMode, NavView);

                // Subscribe to events
                NavView.DisplayModeChanged += NavView_DisplayModeChanged;
                if (Content is FrameworkElement rootElement)
                {
                    rootElement.ActualThemeChanged += MainWindow_ActualThemeChanged;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing MainWindow: {ex.Message}");
                throw; // Re-throw to ensure window creation fails if initialization fails
            }
        }

        // Expose navigation methods
        public void SaveNavigationViewPosition(bool isLeftMode)
        {
            NavigationOrientationHelper.IsLeftModeForElement(isLeftMode, NavView);
        }

        public void UpdateNavigationViewMode(bool isLeftMode)
        {
            NavigationOrientationHelper.UpdateNavigationViewForElement(isLeftMode, NavView);
        }

        private void SetupTheme()
        {
            if (Content is FrameworkElement rootElement)
            {
                ElementTheme savedTheme = ThemeHelper.LoadSavedTheme();
                rootElement.RequestedTheme = savedTheme;
                ThemeHelper.UpdateCaptionButtonsForTheme(this, rootElement.ActualTheme);
            }
        }

        private void MainWindow_ActualThemeChanged(FrameworkElement sender, object args)
        {
            try
            {
                ThemeHelper.UpdateCaptionButtonsForTheme(this, sender.ActualTheme);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling theme change: {ex.Message}");
            }
        }

        private void NavView_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
        {

        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            try
            {
                if (args.IsSettingsSelected)
                {
                    contentFrame.Navigate(typeof(SettingsPage));
                }
                else if (args.SelectedItem is NavigationViewItem selectedItem)
                {
                    switch (selectedItem.Tag.ToString())
                    {
                        case "processes":
                            contentFrame.Navigate(typeof(ProcessesPage));
                            break;
                        case "about":
                            contentFrame.Navigate(typeof(AboutPage));
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling navigation: {ex.Message}");
            }
        }
    }
}