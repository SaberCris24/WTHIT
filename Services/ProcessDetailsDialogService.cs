using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Text;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Plantilla.Models;

namespace Plantilla.Services
{
    public interface IProcessDetailsDialogService
    {
        Task ShowProcessDetailsAsync(ProcessItem process, XamlRoot xamlRoot);
    }

    public class ProcessDetailsDialogService : IProcessDetailsDialogService
    {
        private readonly IVirusScanService _virusScanService;
        private readonly DatabaseService _databaseService;

        public ProcessDetailsDialogService(IVirusScanService virusScanService, DatabaseService databaseService)
        {
            _virusScanService = virusScanService;
            _databaseService = databaseService;
        }

        public async Task ShowProcessDetailsAsync(ProcessItem process, XamlRoot xamlRoot)
        {
            try
            {
                var (path, processDescription) = await GetProcessPathAndDescription(process.ProcessId);
                var processInfo = await _databaseService.GetProcessInfoAsync(process.ProcessName);

                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    process.Information = await _virusScanService.GetVirusTotalDetailsAsync(path);
                }

                var dialog = CreateProcessDetailsDialog(process, processInfo, path, xamlRoot);
                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error showing process details: {ex.Message}");
            }
        }

        private async Task<(string path, string description)> GetProcessPathAndDescription(int processId)
        {
            try
            {
                var proc = Process.GetProcessById(processId);
                return (
                    proc.MainModule?.FileName ?? "Path not available",
                    proc.MainModule?.FileVersionInfo?.FileDescription ?? "Description not available"
                );
            }
            catch (Exception)
            {
                return ("Access denied", "Protected process");
            }
        }

        private ContentDialog CreateProcessDetailsDialog(
            ProcessItem process, 
            ProcessInfo? processInfo, 
            string path,
            XamlRoot xamlRoot
            )
        {
            var mainPanel = new Grid();
            
            // Create a header grid to contain both title and button
            var headerGrid = new Grid();
            
            // Create columns for the header grid
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Create title text block
            var titleBlock = new TextBlock
            {
                Text = "Process Details",
                Style = Application.Current.Resources["TitleTextBlockStyle"] as Style,
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            // Create the VirusTotal info button
            var vtButton = new Button
            {
                Content = new FontIcon 
                { 
                    FontFamily = new FontFamily("Segoe MDL2 Assets"),
                    Glyph = "\uE946",
                    FontSize = 16
                },
                Width = 32,
                Height = 32,
                Padding = new Thickness(0),
                Margin = new Thickness(0),
                RequestedTheme = ((App)Application.Current).ThemeService.GetActualTheme(),
                VerticalAlignment = VerticalAlignment.Center
            };

            // Setup the flyout
            var flyout = new Flyout();
            var flyoutContent = new StackPanel
            {
                Padding = new Thickness(10),
                MaxWidth = 400
            };

            var vtTitle = new TextBlock
            {
                Text = "VirusTotal Information",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var vtInfo = new TextBlock
            {
                Text = process.Information ?? "No VirusTotal information available",
                TextWrapping = TextWrapping.Wrap
            };

            flyoutContent.Children.Add(vtTitle);
            flyoutContent.Children.Add(vtInfo);
            flyout.Content = flyoutContent;
            vtButton.Flyout = flyout;

            // Add title and button to header grid
            Grid.SetColumn(titleBlock, 0);
            Grid.SetColumn(vtButton, 1);
            headerGrid.Children.Add(titleBlock);
            headerGrid.Children.Add(vtButton);

            // Create the main content
            var contentPanel = new StackPanel
            {
                Spacing = 10,
                Padding = new Thickness(10),
                Margin = new Thickness(0)
            };

            // Add the details
            contentPanel.Children.Add(CreateDetailTextBlock($"Process Name: {process.ProcessName}"));
            contentPanel.Children.Add(CreateDetailTextBlock($"Process ID: {process.ProcessId}"));
            contentPanel.Children.Add(CreateDetailTextBlock($"Application Related: {(processInfo?.ApplicationRelated ?? process.ApplicationRelated ?? "Not available")}"));
            contentPanel.Children.Add(CreateDetailTextBlock($"File Location: {path}"));
            contentPanel.Children.Add(CreateDetailTextBlock($"What is Doing this process: {(processInfo?.Description ?? "Not information yet")}"));
            contentPanel.Children.Add(CreateDetailTextBlock($"Is this process resource intensive?: {(processInfo?.IsCpuIntensive ?? "No information available")}"));
            return new ContentDialog
            {
                // Use the custom header grid instead of Title property
                Content = new StackPanel
                {
                    Children =
                    {
                        headerGrid,
                        contentPanel
                    }
                },
                PrimaryButtonText = "Close",
                DefaultButton = ContentDialogButton.Primary,
                RequestedTheme = ((App)Application.Current).ThemeService.GetActualTheme(),
                XamlRoot = xamlRoot,
            };
        }

        private static TextBlock CreateDetailTextBlock(string text)
        {
            return new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap
            };
        }
    }
}