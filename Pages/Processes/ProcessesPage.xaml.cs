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

        private List<ProcessItem> allProcesses; // Static to persist across navigations
        private static bool _processesLoaded = false;

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
                // Verificar cache primero
                if (_processCache.TryGetValue(process.ProcessName, out string cachedInfo))
                {
                    return cachedInfo;
                }

                // Para procesos del sistema, devolver rápidamente
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
                        // Intentar obtener información del registro para Store Apps
                        if (processPath.Contains("WindowsApps"))
                        {
                            appInfo = GetStoreAppName(processPath);
                        }
                        else
                        {
                            // Obtener información del ejecutable
                            var versionInfo = FileVersionInfo.GetVersionInfo(processPath);
                            appInfo = GetBestAppName(versionInfo, processPath);
                        }

                        // Intentar obtener información adicional del registro
                        var registryInfo = GetRegistryAppInfo(process.ProcessName);
                        if (!string.IsNullOrEmpty(registryInfo))
                        {
                            appInfo = registryInfo;
                        }
                    }
                }
                catch
                {
                    // Si falla el acceso al proceso, intentar con el registro
                    appInfo = GetRegistryAppInfo(process.ProcessName) ?? "System Process";
                }

                // Guardar en cache
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
            // Intentar obtener el mejor nombre disponible en orden de preferencia
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
                    
                    // Intentar obtener el nombre amigable del registro
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Families\" + namePart))
                    {
                        if (key?.GetValue("DisplayName") is string displayName)
                        {
                            return displayName;
                        }
                    }
                    
                    // Si no se encuentra en el registro, formatear el nombre del paquete
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
                // Buscar en diferentes ubicaciones del registro
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

        private void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedProcesses = Processes.Where(p => p.IsSelected).ToList();
            if (selectedProcesses.Any())
            {
                foreach (var process in selectedProcesses)
                {
                    process.VirusStatus = "Scanning...";
                }

                ShowSuccess($"Started scanning {selectedProcesses.Count} process(es).");
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

            Processes.Clear();
            foreach (var process in allProcesses)
            {
                Processes.Add(process);
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();

        }

        private void OrderBy_Id(object sender, RoutedEventArgs e)
        {
            if (_isIdSortAscending)
                allProcesses.Sort((a, b) => a.ProcessId.CompareTo(b.ProcessId));
            else
                allProcesses.Sort((a, b) => b.ProcessId.CompareTo(a.ProcessId));

            _isIdSortAscending = !_isIdSortAscending;
            SortByNameIcon_Id.Glyph = _isIdSortAscending ? "\uE70D" : "\uE70E";

            Processes.Clear();
            foreach (var process in allProcesses)
            {
                Processes.Add(process);
            }
            
            GC.Collect();
            GC.WaitForPendingFinalizers();

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

        private void ViewDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ProcessItem process)
            {
                ShowSuccess($"Process Details - Name: {process.ProcessName} (ID: {process.ProcessId})");
            }
        }
    }
}