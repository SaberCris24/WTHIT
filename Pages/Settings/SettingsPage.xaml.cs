using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Plantilla.Helpers;
using System;

namespace Plantilla.Pages.Settings
{
    /// <summary>
    /// Settings page that handles application appearance and behavior configuration
    /// Provides options for theme selection, backdrop effects, and navigation style
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        /// <summary>
        /// Initializes the settings page and sets up required event handlers
        /// </summary>
        public SettingsPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Required;
            
            // Subscribe to the Loaded event for proper initialization timing
            this.Loaded += SettingsPage_Loaded;
        }

        /// <summary>
        /// Handles the page's Loaded event
        /// Ensures settings are initialized after the page is fully loaded
        /// </summary>
        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeSettings();
        }

        /// <summary>
        /// Initializes all settings controls with their current values
        /// Sets up theme, backdrop, and navigation preferences
        /// </summary>
        private void InitializeSettings()
        {
            try
            {
                // Set theme selection based on current theme
                cmbTheme.SelectedIndex = App.CurrentTheme switch
                {
                    ElementTheme.Light => 1,  // Light theme
                    ElementTheme.Dark => 2,   // Dark theme
                    _ => 0                    // System default
                };

                // Set backdrop selection based on current backdrop
                cmbBackdrop.SelectedIndex = App.CurrentBackdrop switch
                {
                    BackdropHelper.BackdropType.MicaAlt => 1,  // Mica Alt
                    BackdropHelper.BackdropType.Acrylic => 2,  // Acrylic
                    _ => 0                                      // Default Mica
                };

                // Set navigation style based on current layout
                var mainWindow = (MainWindow)App.MainWindow;
                cmbNavPosition.SelectedIndex = mainWindow.NavigationViewControl.PaneDisplayMode 
                    == NavigationViewPaneDisplayMode.Top ? 1 : 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles theme selection changes
        /// Updates the application theme when user selects a different option
        /// </summary>
        private void cmbTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    // Convert selection to theme
                    ElementTheme selectedTheme = selectedItem.Tag.ToString() switch
                    {
                        "Light" => ElementTheme.Light,
                        "Dark" => ElementTheme.Dark,
                        _ => ElementTheme.Default
                    };

                    // Apply the selected theme
                    App.SetTheme(selectedTheme);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error changing theme: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles backdrop selection changes
        /// Updates the window backdrop when user selects a different effect
        /// </summary>
        private void cmbBackdrop_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    // Convert selection to backdrop type
                    var backdropType = selectedItem.Tag.ToString() switch
                    {
                        "MicaAlt" => BackdropHelper.BackdropType.MicaAlt,
                        "Acrylic" => BackdropHelper.BackdropType.Acrylic,
                        _ => BackdropHelper.BackdropType.Mica
                    };

                    // Apply the selected backdrop
                    App.SetBackdrop(backdropType);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error changing backdrop: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles navigation style selection changes
        /// Updates the navigation pane position when user changes the layout preference
        /// </summary>
        private void cmbNavPosition_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    var mainWindow = (MainWindow)App.MainWindow;
                    var isLeftMode = (string)selectedItem.Tag == "Left";

                    // Update navigation view layout
                    mainWindow.NavigationViewControl.PaneDisplayMode = isLeftMode
                        ? NavigationViewPaneDisplayMode.Auto
                        : NavigationViewPaneDisplayMode.Top;

                    mainWindow.NavigationViewControl.IsPaneOpen = isLeftMode;

                    // Save the navigation preference
                    mainWindow.SaveNavigationViewPosition(isLeftMode);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error changing navigation position: {ex.Message}");
            }
        }
    }
}