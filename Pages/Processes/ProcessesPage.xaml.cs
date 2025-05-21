using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Plantilla.Models;
using System.ComponentModel;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using Plantilla.Services;

namespace Plantilla.Pages.Processes
{
    public sealed partial class ProcessesPage : Page, INotifyPropertyChanged
    {
        private ObservableCollection<ProcessItem> Processes { get; set; }
        private bool _isProcessSelected;
        private bool _isNameSortAscending = true;
        private bool _isIdSortAscending = true;
        private readonly Dictionary<string, string> _processCache = new Dictionary<string, string>();
        private readonly HashSet<string> _systemProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "svchost", "csrss", "smss", "wininit", "services", "lsass", "winlogon", "system"
        };

        private List<ProcessItem> allProcesses;
        private static bool _processesLoaded = false;
        private DatabaseService _databaseService = new DatabaseService();

        public bool IsProcessSelected
        {
            get => _isProcessSelected;
            private set
            {
                if (_isProcessSelected != value)
                {
                    _isProcessSelected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsProcessSelected)));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ProcessesPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
            Processes = new ObservableCollection<ProcessItem>();
            ProcessListView.ItemsSource = Processes;

            // Initialize database
            _ = _databaseService.InitializeAsync();

            if (allProcesses == null)
            {
                allProcesses = new List<ProcessItem>();
                LoadProcessesToAll();
            }
            else
            {
                RefreshProcessesFromAll();
            }

            this.Loaded += (s, e) =>
            {
                if (!_processesLoaded)
                {
                    LoadProcesses();
                    _processesLoaded = true;
                }
            };
        }

        private void LoadProcessesToAll()
        {
            allProcesses.Clear();
            _processCache.Clear();
            foreach (Process process in Process.GetProcesses())
            {
                allProcesses.Add(new ProcessItem
                {
                    ProcessName = process.ProcessName,
                    ProcessId = process.Id,
                    ApplicationRelated = GetApplicationInfo(process),
                    VirusStatus = "Not Scanned",
                    Information = "Click to view details",
                    IsSelected = false
                });
            }
            RefreshProcessesFromAll();
        }

        private void RefreshProcessesFromAll()
        {
            Processes.Clear();
            foreach (var item in allProcesses)
            {
                Processes.Add(item);
            }
        }

        private void LoadProcesses()
        {
            try
            {
                LoadingRing.IsActive = true;
                Processes.Clear();
                _processCache.Clear();

                foreach (Process process in Process.GetProcesses())
                {
                    Processes.Add(new ProcessItem
                    {
                        ProcessName = process.ProcessName,
                        ProcessId = process.Id,
                        ApplicationRelated = GetApplicationInfo(process),
                        VirusStatus = "Not Scanned",
                        Information = "Click to view details",
                        IsSelected = false
                    });
                }

                UpdateSelectionState();
            }
            catch (Exception ex)
            {
                ShowError($"Error loading processes: {ex.Message}");
            }
            finally
            {
                LoadingRing.IsActive = false;
            }
        }

        private string GetApplicationInfo(Process process)
        {
            try
            {
                // Check cache first
                if (_processCache.TryGetValue(process.ProcessName, out string cachedInfo))
                {
                    return cachedInfo;
                }

                // For system processes, return quickly
                if (_systemProcesses.Contains(process.ProcessName))
                {
                    var result = $"Windows {process.ProcessName}";
                    _processCache[process.ProcessName] = result;
                    return result;
                }

                string appInfo = "Unknown";

                try
                {
                    string processPath = process.MainModule?.FileName ?? string.Empty;
                    if (!string.IsNullOrEmpty(processPath))
                    {
                        // Try to get info from registry for Store Apps
                        if (processPath.Contains("WindowsApps"))
                        {
                            appInfo = GetStoreAppName(processPath);
                        }
                        else
                        {
                            // Get info from executable
                            var versionInfo = FileVersionInfo.GetVersionInfo(processPath);
                            appInfo = GetBestAppName(versionInfo, processPath);
                        }

                        // Try to get additional info from registry
                        var registryInfo = GetRegistryAppInfo(process.ProcessName);
                        if (!string.IsNullOrEmpty(registryInfo))
                        {
                            appInfo = registryInfo;
                        }
                    }
                }
                catch
                {
                    // If process access fails, try registry
                    appInfo = GetRegistryAppInfo(process.ProcessName) ?? "System Process";
                }

                // Save to cache
                _processCache[process.ProcessName] = appInfo;
                return appInfo;
            }
            catch (Exception)
            {
                return "Unknown";
            }
        }

        private string GetBestAppName(FileVersionInfo versionInfo, string processPath)
        {
            return new[]
            {
                versionInfo.ProductName,
                versionInfo.FileDescription,
                versionInfo.CompanyName,
                Path.GetFileNameWithoutExtension(processPath)
            }
            .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? "Unknown";
        }

        private string GetStoreAppName(string processPath)
        {
            try
            {
                var pathParts = processPath.Split('\\');
                var appIndex = Array.FindIndex(pathParts, x => x.Equals("WindowsApps", StringComparison.OrdinalIgnoreCase));
                if (appIndex >= 0 && pathParts.Length > appIndex + 1)
                {
                    var appPart = pathParts[appIndex + 1];
                    var namePart = appPart.Split('_')[0];

                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Families\" + namePart))
                    {
                        if (key?.GetValue("DisplayName") is string displayName)
                        {
                            return displayName;
                        }
                    }

                    return $"Store: {FormatAppName(namePart)}";
                }
            }
            catch { }

            return "Windows Store App";
        }

        private string GetRegistryAppInfo(string processName)
        {
            try
            {
                var registryPaths = new[]
                {
                    @"Software\Microsoft\Windows\CurrentVersion\App Paths\",
                    @"Software\Microsoft\Windows\CurrentVersion\Uninstall\",
                    @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\"
                };

                foreach (var basePath in registryPaths)
                {
                    using (var baseKey = Registry.LocalMachine.OpenSubKey(basePath))
                    {
                        if (baseKey == null) continue;

                        foreach (var keyName in baseKey.GetSubKeyNames())
                        {
                            using (var key = baseKey.OpenSubKey(keyName))
                            {
                                if (key == null) continue;

                                var displayName = key.GetValue("DisplayName") as string;
                                var exeName = Path.GetFileNameWithoutExtension(keyName);

                                if (!string.IsNullOrEmpty(displayName) &&
                                    exeName.Equals(processName, StringComparison.OrdinalIgnoreCase))
                                {
                                    return displayName;
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            return null;
        }

        private string FormatAppName(string name)
        {
            return System.Text.RegularExpressions.Regex.Replace(
                name,
                "([A-Z])",
                " $1",
                System.Text.RegularExpressions.RegexOptions.Compiled
            ).Trim();
        }

        private void ShowError(string message)
        {
            StatusInfoBar.Message = message;
            StatusInfoBar.Severity = InfoBarSeverity.Error;
            StatusInfoBar.IsOpen = true;
        }

        private void ShowSuccess(string message)
        {
            StatusInfoBar.Message = message;
            StatusInfoBar.Severity = InfoBarSeverity.Success;
            StatusInfoBar.IsOpen = true;
        }

        private void UpdateSelectionState()
        {
            IsProcessSelected = Processes.Any(p => p.IsSelected);
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadProcesses();
        }

        private const string VirusTotalApiKey = "1234"; // Here the api key

        private async void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedProcesses = Processes.Where(p => p.IsSelected).ToList();
            if (selectedProcesses.Any())
            {
                foreach (var process in selectedProcesses)
                {
                    process.VirusStatus = "Scanning...";
                    try
                    {
                        var proc = Process.GetProcessById(process.ProcessId);
                        var path = proc.MainModule?.FileName;
                        if (!string.IsNullOrEmpty(path) && File.Exists(path))
                        {
                            process.VirusStatus = await GetVirusTotalReportAsync(path, VirusTotalApiKey);
                        }
                        else
                        {
                            process.VirusStatus = "No file path";
                        }
                    }
                    catch (Exception ex)
                    {
                        process.VirusStatus = $"Error: {ex.Message}";
                    }
                }

                ShowSuccess($"Scanned {selectedProcesses.Count} process(es).");
            }
        }

        private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (string.IsNullOrWhiteSpace(args.QueryText))
            {
                LoadProcesses();
                return;
            }

            var filtered = Processes
                .Where(p => p.ProcessName.Contains(args.QueryText, StringComparison.OrdinalIgnoreCase))
                .ToList();

            Processes.Clear();
            foreach (var process in filtered)
            {
                Processes.Add(process);
            }
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                if (string.IsNullOrWhiteSpace(sender.Text))
                {
                    LoadProcesses();
                    return;
                }

                var suggestions = Processes
                    .Where(p => p.ProcessName.Contains(sender.Text, StringComparison.OrdinalIgnoreCase))
                    .Select(p => p.ProcessName)
                    .Distinct()
                    .Take(5)
                    .ToList();

                sender.ItemsSource = suggestions;
            }
        }

        private void SearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is string processName)
            {
                sender.Text = processName;
            }
        }

        private void OrderBy_Name(object sender, RoutedEventArgs e)
        {
            if (_isNameSortAscending)
                allProcesses.Sort((a, b) => string.Compare(a.ProcessName, b.ProcessName, StringComparison.OrdinalIgnoreCase));
            else
                allProcesses.Sort((a, b) => string.Compare(b.ProcessName, a.ProcessName, StringComparison.OrdinalIgnoreCase));

            _isNameSortAscending = !_isNameSortAscending;
            SortByNameIcon_Name.Glyph = _isNameSortAscending ? "\uE70D" : "\uE70E";

            RefreshProcessesFromAll();
        }

        private void OrderBy_Id(object sender, RoutedEventArgs e)
        {
            if (_isIdSortAscending)
                allProcesses.Sort((a, b) => a.ProcessId.CompareTo(b.ProcessId));
            else
                allProcesses.Sort((a, b) => b.ProcessId.CompareTo(a.ProcessId));

            _isIdSortAscending = !_isIdSortAscending;
            SortByNameIcon_Id.Glyph = _isIdSortAscending ? "\uE70D" : "\uE70E";

            RefreshProcessesFromAll();
        }

        private void ProcessCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            UpdateSelectionState();
        }

        private void ProcessCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateSelectionState();
        }

        private void ProcessListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is ProcessItem process)
            {
                ShowSuccess($"Process: {process.ProcessName} (ID: {process.ProcessId})");
            }
        }

        private async Task<string> GetVirusTotalReportAsync(string filePath, string apiKey)
        {
            try
            {
                using var sha256 = SHA256.Create();
                using var stream = File.OpenRead(filePath);
                var hashBytes = sha256.ComputeHash(stream);
                var hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("x-apikey", apiKey);
                var url = $"https://www.virustotal.com/api/v3/files/{hash}";
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return "Not found on VT";

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var stats = doc.RootElement
                    .GetProperty("data")
                    .GetProperty("attributes")
                    .GetProperty("last_analysis_stats");

                int malicious = stats.GetProperty("malicious").GetInt32();
                int undetected = stats.GetProperty("undetected").GetInt32();

                return $"Malicious: {malicious}, Undetected: {undetected}";
            }
            catch (Exception ex)
            {
                return $"VT Error: {ex.Message}";
            }
        }

        private async Task<string> GetVirusTotalDetailsAsync(string filePath, string apiKey)
        {
            try
            {
                using var sha256 = SHA256.Create();
                using var stream = File.OpenRead(filePath);
                var hashBytes = sha256.ComputeHash(stream);
                var hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("x-apikey", apiKey);
                var url = $"https://www.virustotal.com/api/v3/files/{hash}";
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return "No VirusTotal info found.";

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var attr = doc.RootElement.GetProperty("data").GetProperty("attributes");

                string name = attr.TryGetProperty("meaningful_name", out var n) ? n.GetString() : null;
                string type = attr.TryGetProperty("type_description", out var t) ? t.GetString() : null;
                string desc = attr.TryGetProperty("description", out var d) ? d.GetString() : null;
                string tags = attr.TryGetProperty("tags", out var tagsProp) && tagsProp.ValueKind == JsonValueKind.Array
                    ? string.Join(", ", tagsProp.EnumerateArray().Select(x => x.GetString()))
                    : null;
                string altName = attr.TryGetProperty("names", out var namesProp) && namesProp.ValueKind == JsonValueKind.Array && namesProp.GetArrayLength() > 0
                    ? namesProp[0].GetString()
                    : null;

                if (string.IsNullOrWhiteSpace(desc))
                {
                    desc = altName ?? "N/A";
                }

                return $"Name: {name ?? "N/A"}\nType: {type ?? "N/A"}\nDescription: {desc}\nTags: {tags ?? "N/A"}";
            }
            catch (Exception ex)
            {
                return $"VT Error: {ex.Message}";
            }
        }

        private async void ViewDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ProcessItem process)
            {
                try
                {
                    var currentWindow = App.MainWindow;
                    if (currentWindow == null)
                    {
                        ShowError("Cannot show dialog - Window is null");
                        return;
                    }

                    string path;
                    string processDescription;

                    try
                    {
                        var proc = Process.GetProcessById(process.ProcessId);
                        path = proc.MainModule?.FileName ?? "Path not available";
                        processDescription = proc.MainModule?.FileVersionInfo?.FileDescription ?? "Description not available";
                    }
                    catch (Exception)
                    {
                        path = "Access denied";
                        processDescription = "Protected process";
                    }

                    // Obtener información de la base de datos
                    var processInfo = await _databaseService.GetProcessInfoAsync(process.ProcessName);

                    var dialog = new ContentDialog
                    {
                        Title = "Process Details",
                        PrimaryButtonText = "Close",
                        DefaultButton = ContentDialogButton.Primary,
                        XamlRoot = currentWindow.Content.XamlRoot, // Usar el XamlRoot de la ventana principal
                        Content = new StackPanel
                        {
                            Spacing = 10,
                            Padding = new Thickness(10),
                            Children =
                            {
                                new TextBlock 
                                { 
                                    Text = $"Process Name: {process.ProcessName}",
                                    TextWrapping = TextWrapping.Wrap
                                },
                                new TextBlock 
                                { 
                                    Text = $"Process ID: {process.ProcessId}",
                                    TextWrapping = TextWrapping.Wrap
                                },
                                new TextBlock 
                                { 
                                    Text = $"Application Related: {(processInfo?.ApplicationRelated ?? process.ApplicationRelated ?? "Not available")}",
                                    TextWrapping = TextWrapping.Wrap
                                },
                                new TextBlock 
                                { 
                                    Text = $"File Location: {(processInfo?.FileLocation ?? path)}",
                                    TextWrapping = TextWrapping.Wrap
                                },
                                new TextBlock 
                                { 
                                    Text = $"What is Doing this process: {(processInfo?.Description ?? "Not information yet")}",
                                    TextWrapping = TextWrapping.Wrap
                                },
                                new TextBlock 
                                { 
                                    Text = $"Is this process CPU intensive?: {(processInfo?.IsCpuIntensive == true ? "Yes" : "No")}",
                                    TextWrapping = TextWrapping.Wrap
                                }
                            }
                        }
                    };

                    await dialog.ShowAsync();
                }
                catch (Exception ex)
                {
                    ShowError($"Error: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"ViewDetails error: {ex}");
                }
            }
        }
    }
}