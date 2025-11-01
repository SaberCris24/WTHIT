using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Plantilla.Helpers;
using Plantilla.Pages;
using Plantilla.Pages.About;
using Plantilla.Pages.Home;
using Plantilla.Pages.Processes;
using Plantilla.Pages.Settings;
using System;

namespace Plantilla
{
    /// <summary>
    /// Main window of the application that handles navigation, theme setup, and window configuration
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        #region Properties
        /// <summary>
        /// Gets the NavigationView control used for app navigation
        /// </summary>
        public NavigationView NavigationViewControl => NavView;
        
        /// <summary>
        /// Helper class for managing navigation
        /// </summary>
        private NavigationHelper _navigationHelper;
        #endregion

        /// <summary>
        /// Initializes a new instance of the MainWindow class with specified minimum dimensions
        /// </summary>
        /// <param name="MinWidth">Minimum width of the window</param>
        /// <param name="MinHeight">Minimum height of the window</param>
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

                // Navigate to Home page by default
                contentFrame.Navigate(typeof(HomePage));

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

        /// <summary>
        /// Saves the current navigation view position
        /// </summary>
        /// <param name="isLeftMode">Whether the navigation view should be in left mode</param>
        public void SaveNavigationViewPosition(bool isLeftMode)
        {
            NavigationOrientationHelper.IsLeftModeForElement(isLeftMode, NavView);
        }

        /// <summary>
        /// Updates the navigation view mode
        /// </summary>
        /// <param name="isLeftMode">Whether the navigation view should be in left mode</param>
        public void UpdateNavigationViewMode(bool isLeftMode)
        {
            NavigationOrientationHelper.UpdateNavigationViewForElement(isLeftMode, NavView);
        }

        /// <summary>
        /// Sets up the theme for the application based on saved preferences
        /// </summary>
        private void SetupTheme()
        {
            if (Content is FrameworkElement rootElement)
            {
                ElementTheme savedTheme = ThemeHelper.LoadSavedTheme();
                rootElement.RequestedTheme = savedTheme;
                ThemeHelper.UpdateCaptionButtonsForTheme(this, rootElement.ActualTheme);
            }
        }

        /// <summary>
        /// Handles the ActualThemeChanged event of the main window
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="args">Event arguments</param>
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

        /// <summary>
        /// Handles the DisplayModeChanged event of the NavigationView
        /// </summary>
        /// <param name="sender">The NavigationView control</param>
        /// <param name="args">Event arguments containing display mode information</param>
        private void NavView_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
        {

        }

        /// <summary>
        /// Handles the SelectionChanged event of the NavigationView
        /// Navigates to the appropriate page based on the selected item
        /// </summary>
        /// <param name="sender">The NavigationView control</param>
        /// <param name="args">Event arguments containing selection information</param>
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
                        case "home":
                            contentFrame.Navigate(typeof(HomePage));
                            break;
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