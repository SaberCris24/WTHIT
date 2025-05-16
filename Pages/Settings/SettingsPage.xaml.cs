using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Plantilla.Pages.Settings
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();
            ((App)Application.Current).ThemeService.SetThemeComboBoxDefaultItem(cmbTheme);
        }

        private void cmbTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ((App)Application.Current).ThemeService.OnThemeComboBoxSelectionChanged(sender);
        }
    }
}