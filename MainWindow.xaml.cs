using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using DevWinUI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Plantilla
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
   public sealed partial class MainWindow : Window
    {
        private List<string> sugerencias = new List<string>();

        public MainWindow(int MinWidth, int MinHeight)
        {
            this.InitializeComponent();
            this.ExtendsContentIntoTitleBar = true;

            AppWindow.SetPresenter(AppWindowPresenterKind.Overlapped);

            AppWindow.Title = "WTHIT";

            // Set the window size (including borders)
            AppWindow.Resize(new Windows.Graphics.SizeInt32(1000, 800));

            // Set the window position on screen
            AppWindow.Move(new Windows.Graphics.PointInt32(50, 50));

            // Set the preferred theme for the title bar
            AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;

            // Set the taskbar icon (displayed in the taskbar)
            AppWindow.SetTaskbarIcon("Assets/Tiles/GalleryIcon.ico");

            // Set the title bar icon (displayed in the window's title bar)
            AppWindow.SetTitleBarIcon("Assets/Tiles/GalleryIcon.ico");

            OverlappedPresenter presenter = OverlappedPresenter.Create();
            presenter.PreferredMinimumWidth = MinWidth;
            presenter.PreferredMinimumHeight = MinHeight;

            AppWindow.SetPresenter(presenter);

            sugerencias = new List<string>
            {
                "explorer.exe",
                "msedge.exe",
                "notepad.exe",
            };
        }

         private void OnThemeRadioButtonChecked(object sender, RoutedEventArgs e)
            {
                ((App)Application.Current).ThemeService.OnThemeRadioButtonChecked(sender);
            }

            private void cmbTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
            {
                ((App)Application.Current).ThemeService.OnThemeComboBoxSelectionChanged(sender);
            }

        private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.QueryText != string.Empty)
            {
                // Lógica para procesar la consulta
            }
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var filteredItems = string.IsNullOrEmpty(sender.Text) ? 
                    new string[] { } : 
                    sugerencias
                        .Where(p => p.ToLower().Contains(sender.Text.ToLower()))
                        .ToArray();
                
                sender.ItemsSource = filteredItems;
            }
        }

        private void SearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            sender.Text = args.SelectedItem.ToString();
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            StackPanel contentPanel = new StackPanel();

            TextBlock infoText = new TextBlock
            {
                Text = "WTHIT\nVersion 1.0\n\nDeveloped by: Geriberto\n",
                TextWrapping = TextWrapping.Wrap
            };
            contentPanel.Children.Add(infoText);

            TextBlock linkText = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap
            };

            Hyperlink hyperlink = new Hyperlink
            {
                NavigateUri = new Uri("https://github.com/SaberCris24/WTHIT")
            };

            Run linkRun = new Run
            {
                Text = "GitHub: https://github.com/SaberCris24/WTHIT"
            };

            hyperlink.Inlines.Add(linkRun);
            
            linkText.Inlines.Add(hyperlink);

            contentPanel.Children.Add(linkText);

            ContentDialog aboutDialog = new ContentDialog
            {
                Title = "About",
                Content = contentPanel,
                CloseButtonText = "Close",
                RequestedTheme = ((App)Application.Current).ThemeService.GetActualTheme(),
                XamlRoot = this.Content.XamlRoot
            };

            _ = aboutDialog.ShowAsync();
        }

        // Método para el click del hyperlink
        private async void Hyperlink_Click(Hyperlink sender, HyperlinkClickEventArgs args)
        {

            if (sender.NavigateUri != null)
            {
               await Windows.System.Launcher.LaunchUriAsync(sender.NavigateUri);
            }
        }
    }
}