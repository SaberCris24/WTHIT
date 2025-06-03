using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Plantilla.Pages.Settings
{
    /// <summary>
    /// Settings page for the application
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        /// <summary>
        /// Initializes settings page and theme selection
        /// </summary>
        public SettingsPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
            InitializeSettings();
        }

        /// <summary>
        /// Initialize settings controls with current values
        /// </summary>
        private void InitializeSettings()
        {
            // Initialize theme selection
            ((App)Application.Current).ThemeService.SetThemeComboBoxDefaultItem(cmbTheme);

            // Initialize navigation position selection
            var mainWindow = (MainWindow)App.MainWindow;
            if (mainWindow.NavigationViewControl.PaneDisplayMode == NavigationViewPaneDisplayMode.Top)
            {
                cmbNavPosition.SelectedIndex = 1; // Top
            }
            else
            {
                cmbNavPosition.SelectedIndex = 0; // Left
            }
        }

        /// <summary>
        /// Handles theme selection changes
        /// </summary>
        private void cmbTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ((App)Application.Current).ThemeService.OnThemeComboBoxSelectionChanged(sender);
        }

        /// <summary>
        /// Handles navigation position changes
        /// </summary>
        private void cmbNavPosition_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                var mainWindow = (MainWindow)App.MainWindow;
                var isLeftMode = (string)selectedItem.Tag == "Left";

                if (isLeftMode)
                {
                    mainWindow.NavigationViewControl.PaneDisplayMode = NavigationViewPaneDisplayMode.Auto;
                    mainWindow.NavigationViewControl.IsPaneOpen = true;
                }
                else
                {
                    mainWindow.NavigationViewControl.PaneDisplayMode = NavigationViewPaneDisplayMode.Top;
                    mainWindow.NavigationViewControl.IsPaneOpen = false;
                }

                mainWindow.SaveNavigationViewPosition(isLeftMode);
            }
        }
    }
}