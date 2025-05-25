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
            ((App)Application.Current).ThemeService.SetThemeComboBoxDefaultItem(cmbTheme);
        }

        /// <summary>
        /// Handles theme selection changes
        /// </summary>
        private void cmbTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ((App)Application.Current).ThemeService.OnThemeComboBoxSelectionChanged(sender);
        }
    }
}