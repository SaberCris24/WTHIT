using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Plantilla.Pages.About;
using Plantilla.Pages.Processes;
using Plantilla.Pages.Settings;

namespace Plantilla
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow(int MinWidth, int MinHeight)
        {
            this.InitializeComponent();
            this.ExtendsContentIntoTitleBar = true;

            AppWindow.Title = "WTHIT";
            AppWindow.SetTaskbarIcon("Assets/icon.png");
            AppWindow.SetTitleBarIcon("Assets/icon.png");

            OverlappedPresenter presenter = OverlappedPresenter.Create();
            presenter.PreferredMinimumWidth = MinWidth;
            presenter.PreferredMinimumHeight = MinHeight;
            AppWindow.SetPresenter(presenter);

            // Navigate to Processes page by default
            contentFrame.Navigate(typeof(ProcessesPage));
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem selectedItem)
            {
                switch (selectedItem.Tag.ToString())
                {
                    case "settings":
                        contentFrame.Navigate(typeof(SettingsPage));
                        break;
                    case "about":
                        contentFrame.Navigate(typeof(AboutPage));
                        break;
                    case "processes":
                        contentFrame.Navigate(typeof(ProcessesPage));
                        break;
                }
            }
        }
    }
}