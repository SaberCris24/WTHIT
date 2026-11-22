using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;
using System;
using System.Threading.Tasks;

namespace WTHIT.Pages.About
{
    /// <summary>
    /// About page displaying application information - optimized
    /// </summary>
    public sealed partial class AboutPage : Page, IDisposable
    {
        private bool _disposed;

        /// <summary>Displays the version embedded in the application assembly at build time.</summary>
        public string AppVersion { get; } = typeof(AboutPage).Assembly.GetName().Version?.ToString() ?? "Unknown";

        public AboutPage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Disabled;
            Unloaded += OnPageUnloaded;
        }

        private void OnPageUnloaded(object sender, RoutedEventArgs e)
        {
            CopyGitButton.Click -= CopyGitCommand_Click;
        }

        private async void CopyGitCommand_Click(object sender, RoutedEventArgs e)
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(GitCommandText.Text);
            Clipboard.SetContent(dataPackage);

            if (sender is Button button)
            {
                var originalText = CopyButtonText.Text;
                CopyButtonText.Text = "Copied!";
                button.IsEnabled = false;

                await Task.Delay(2000);

                if (!_disposed)
                {
                    CopyButtonText.Text = originalText;
                    button.IsEnabled = true;
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            Unloaded -= OnPageUnloaded;
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            Dispose();
        }
    }
}
