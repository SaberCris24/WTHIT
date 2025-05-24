using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
            XamlRoot xamlRoot)
        {
            return new ContentDialog
            {
                Title = "Process Details",
                PrimaryButtonText = "Close",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = xamlRoot,
                Content = new StackPanel
                {
                    Spacing = 10,
                    Padding = new Thickness(10),
                    Children =
                    {
                        CreateDetailTextBlock($"Process Name: {process.ProcessName}"),
                        CreateDetailTextBlock($"Process ID: {process.ProcessId}"),
                        CreateDetailTextBlock($"Application Related: {(processInfo?.ApplicationRelated ?? process.ApplicationRelated ?? "Not available")}"),
                        CreateDetailTextBlock($"File Location: {path}"),
                        CreateDetailTextBlock($"What is Doing this process: {(processInfo?.Description ?? "Not information yet")}"),
                        CreateDetailTextBlock($"Is this process resource intensive?: {(processInfo?.IsCpuIntensive == true ? "Yes" : "No")}"),
                        CreateDetailTextBlock($"Virus Total Information:\n{process.Information}")
                    }
                }
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