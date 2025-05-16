using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Plantilla.Models;

namespace Plantilla.Pages.Processes
{
    public sealed partial class ProcessesPage : Page
    {
        private List<ProcessItem> allProcesses;
        private bool isSortedAscending = true;

        public ProcessesPage()
        {
            this.InitializeComponent();
            allProcesses = new List<ProcessItem>();
            this.Loaded += LoadProcesses;
        }

        private void LoadProcesses(object sender, RoutedEventArgs e)
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
            UpdateSortingIcon(sender);
            isSortedAscending = !isSortedAscending;

            try
            {
                allProcesses = isSortedAscending
                    ? allProcesses.OrderBy(p => p.ProcessId).ToList()
                    : allProcesses.OrderByDescending(p => p.ProcessId).ToList();

                ProcessListView.ItemsSource = allProcesses;
            }
            catch (Exception ex)
            {
                ShowError($"Error sorting processes: {ex.Message}");
            }
        }

        private void OrderBy_Name(object sender, RoutedEventArgs e)
        {
            UpdateSortingIcon(sender);
            isSortedAscending = !isSortedAscending;
            LoadProcesses(sender, e);
        }

        private void UpdateSortingIcon(object sender)
        {
            if (sender is Button button)
            {
                if (button.Content is StackPanel stackPanel)
                {
                    if (stackPanel.Children.OfType<FontIcon>().FirstOrDefault() is FontIcon icon)
                    {
                        icon.Glyph = isSortedAscending ? "\uE96E" : "\uE96D";
                    }
                }
            }
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
                XamlRoot = this.XamlRoot
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
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                string searchText = sender.Text.ToLower();

                var filteredProcesses = allProcesses
                    .Where(p => p.ProcessName.ToLower().StartsWith(searchText))
                    .Select(p => p.ProcessName)
                    .Distinct()
                    .ToList();

                sender.ItemsSource = filteredProcesses;
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
    }
}