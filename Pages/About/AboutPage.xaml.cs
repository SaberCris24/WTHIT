using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;
using CommunityToolkit.WinUI.Controls;

namespace Plantilla.Pages.About
{
    /// <summary>
    /// Page that displays information about the application, including repository details
    /// </summary>
    public sealed partial class AboutPage : Page
    {
        /// <summary>
        /// Initializes a new instance of the AboutPage class
        /// </summary>
        public AboutPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Handles the click event for the copy git command button
        /// Copies the git clone command to the clipboard and temporarily changes the button text
        /// </summary>
        /// <param name="sender">The button that triggered the event</param>
        /// <param name="e">Event data for the click event</param>
        private void CopyGitCommand_Click(object sender, RoutedEventArgs e)
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText("git clone https://github.com/SaberCris24/WTHIT");
            Clipboard.SetContent(dataPackage);

            if (sender is SettingsCard card)
            {
                // Save the original header safely
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