using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;
using CommunityToolkit.WinUI.Controls;

namespace Plantilla.Pages.About
{
    public sealed partial class AboutPage : Page
    {
        public AboutPage()
        {
            this.InitializeComponent();
        }

        private void CopyGitCommand_Click(object sender, RoutedEventArgs e)
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText("git clone https://github.com/SaberCris24/WTHIT");
            Clipboard.SetContent(dataPackage);

            if (sender is SettingsCard card)
            {
                // Guardar el header original de manera segura
                string headerText = card.Header?.ToString() ?? "Clone Repository";
                card.Header = "Copied!";
                
                DispatcherQueue.TryEnqueue(() =>
                {
                    card.Header = headerText;
                });
            }
        }
    }
}