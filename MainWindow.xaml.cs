using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace Plantilla
{
    public sealed partial class MainWindow : Window
    {
        private List<ProcessItem> allProcesses;
        
        private bool isSortedAscending = true;


        public MainWindow(int MinWidth, int MinHeight)
        {
            this.InitializeComponent();
            this.ExtendsContentIntoTitleBar = true;

            AppWindow.Title = "WTHIT";
            AppWindow.SetTaskbarIcon("Assets/icon.png");
            AppWindow.SetTitleBarIcon("Assets/icon.png");

            // ((App)Application.Current).ThemeService.SetThemeComboBoxDefaultItem(cmbTheme);

            OverlappedPresenter presenter = OverlappedPresenter.Create();
            presenter.PreferredMinimumWidth = MinWidth;
            presenter.PreferredMinimumHeight = MinHeight;
            AppWindow.SetPresenter(presenter);

            allProcesses = new List<ProcessItem>();
            LoadProcesses();
        }
        private void OnThemeRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).ThemeService.OnThemeRadioButtonChecked(sender);
        }

        private void cmbTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ((App)Application.Current).ThemeService.OnThemeComboBoxSelectionChanged(sender);
        }

        private void ShowSettings()
         {
             contentFrame.Navigate(typeof(SettingsPage));
         }
        private void LoadProcesses()
        {
            try
            {
                allProcesses = Process.GetProcesses()
                    .Select(p => new ProcessItem
                    {
                        ProcessName = p.ProcessName,
                        ProcessId = p.Id,
                        ApplicationRelated = DetermineApplicationRelation(p.ProcessName),
                        VirusStatus = "Scanning...",
                        Information = "Click for details"
                    })
                    .ToList();

                    allProcesses = isSortedAscending
                    ? allProcesses.OrderBy(p => p.ProcessName).ToList()
                    : allProcesses.OrderByDescending(p => p.ProcessName).ToList();

                ProcessListView.ItemsSource = allProcesses;
            }
            catch (Exception ex)
            {
                ShowError($"Error loading processes: {ex.Message}");
            }
        }

        private string DetermineApplicationRelation(string processName)
        {
            var knownApps = new Dictionary<string, string>
            {
                { "explorer", "Windows Explorer" },
                { "msedge", "Microsoft Edge" },
                { "notepad", "Windows Notepad" }
                // Aqui agregar mas apps
            };

            foreach (var app in knownApps)
            {
                if (processName.ToLower().Contains(app.Key))
                {
                    return app.Value;
                }
            }

            return "Unknown Application";
        }

        private void ViewDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ProcessItem process)
            {
                ShowProcessDetails(process);
            }
        }

        private void OrderBy_Id(object sender, RoutedEventArgs e)
        {
            
            var button = sender as Button;
            if (button != null)
            {
                var stackPanel = button.Content as StackPanel;
                if (stackPanel != null)
                {
                    var icon = stackPanel.Children.OfType<FontIcon>().FirstOrDefault();
                    if (icon != null)
                    {
                        icon.Glyph = isSortedAscending ? "\uE96E" : "\uE96D";
                    }
                }
            }

            isSortedAscending = !isSortedAscending; // Toggle for next time

            try
            {
                allProcesses = Process.GetProcesses()
                    .Select(p => new ProcessItem
                    {
                        ProcessName = p.ProcessName,
                        ProcessId = p.Id,
                        ApplicationRelated = DetermineApplicationRelation(p.ProcessName),
                        VirusStatus = "Scanning...",
                        Information = "Click for details"
                    })
                    .ToList();

                allProcesses = isSortedAscending
                ? allProcesses.OrderBy(p => p.ProcessId).ToList()
                : allProcesses.OrderByDescending(p => p.ProcessId).ToList();

                ProcessListView.ItemsSource = allProcesses;
            }
            catch (Exception ex)
            {
                ShowError($"Error loading processes: {ex.Message}");
            }
        }

        private void OrderBy_Name(object sender, RoutedEventArgs e)
        {   // Update icon
            var button = sender as Button;
            if (button != null)
            {
                var stackPanel = button.Content as StackPanel;
                if (stackPanel != null)
                {
                    var icon = stackPanel.Children.OfType<FontIcon>().FirstOrDefault();
                    if (icon != null)
                    {
                        icon.Glyph = isSortedAscending ? "\uE96E" : "\uE96D";
                    }
                }
            }

            isSortedAscending = !isSortedAscending; // Toggle for next time

            LoadProcesses();

        }

        private async void ShowProcessDetails(ProcessItem process)
        {
            var detailsPanel = new StackPanel { Spacing = 10 };

            detailsPanel.Children.Add(new TextBlock
            {
                Text = $"Process Name: {process.ProcessName}",
                TextWrapping = TextWrapping.Wrap
            });

            detailsPanel.Children.Add(new TextBlock
            {
                Text = $"Process ID: {process.ProcessId}",
                TextWrapping = TextWrapping.Wrap
            });

            detailsPanel.Children.Add(new TextBlock
            {
                Text = $"Application: {process.ApplicationRelated}",
                TextWrapping = TextWrapping.Wrap
            });

            detailsPanel.Children.Add(new TextBlock
            {
                Text = $"Security Status: {process.VirusStatus}",
                TextWrapping = TextWrapping.Wrap
            });

            ContentDialog dialog = new ContentDialog
            {
                Title = "Process Details",
                Content = detailsPanel,
                CloseButtonText = "Close",
                RequestedTheme = ((App)Application.Current).ThemeService.GetActualTheme(),
                XamlRoot = this.Content.XamlRoot
            };

            await dialog.ShowAsync();
        }

        private async void ShowError(string message)
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = "Error",
                Content = message,
                CloseButtonText = "OK",
                RequestedTheme = ((App)Application.Current).ThemeService.GetActualTheme(),
                XamlRoot = this.Content.XamlRoot
            };

            await dialog.ShowAsync();
        }


        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                string searchText = sender.Text.ToLower();

                // Filtrar procesos que coincidan con el texto de búsqueda
                var filteredProcesses = allProcesses
                    .Where(p => p.ProcessName.ToLower().StartsWith(searchText))
                    .Select(p => p.ProcessName)
                    .Distinct()
                    .ToList();

                // Actualizar las sugerencias
                sender.ItemsSource = filteredProcesses;

                // Actualizar la lista de procesos en tiempo real
                FilterProcesses(searchText);
            }
        }

        private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (!string.IsNullOrEmpty(args.QueryText))
            {
                FilterProcesses(args.QueryText);
            }
            else
            {
                // Si la búsqueda está vacía, mostrar todos los procesos
                ProcessListView.ItemsSource = allProcesses;
            }
        }

        private void FilterProcesses(string searchText)
        {
            searchText = searchText.ToLower();
            
            var filteredProcesses = allProcesses
                .Where(p => p.ProcessName.ToLower().Contains(searchText) ||
                           p.ApplicationRelated.ToLower().Contains(searchText))
                .ToList();

            ProcessListView.ItemsSource = filteredProcesses;
        }

        private void SearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem != null)
            {
                sender.Text = args.SelectedItem.ToString();
                FilterProcesses(sender.Text);
            }
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem selectedItem)
            {
                switch (selectedItem.Tag.ToString())
                {
                    case "settings":
                        ShowSettings();
                        break;
                    case "about":
                        ShowAbout();
                        break;
                    case "processes":

                        break;
                }
            }
        }


        private void ShowAbout()
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
                Text = "GitHub Repository"
            };

            hyperlink.Inlines.Add(linkRun);
            linkText.Inlines.Add(hyperlink);
            contentPanel.Children.Add(linkText);

            ContentDialog aboutDialog = new ContentDialog
            {
                Title = "About WTHIT",
                Content = contentPanel,
                CloseButtonText = "Close",
                RequestedTheme = ((App)Application.Current).ThemeService.GetActualTheme(),
                XamlRoot = this.Content.XamlRoot
            };

            _ = aboutDialog.ShowAsync();
        }
    }

    public class ProcessItem
    {
        public string ProcessName { get; set; }
        public int ProcessId { get; set; }
        public string ApplicationRelated { get; set; }
        public string VirusStatus { get; set; }
        public string Information { get; set; }
    }
}