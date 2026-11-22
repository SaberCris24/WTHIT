using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Win32;
using WTHIT.Core;
using WTHIT.Models;
using WTHIT.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace WTHIT.Pages.Home
{
    /// <summary>
    /// Home dashboard page displaying system overview, uptime, and top processes.
    /// Optimized for lower memory usage, safer timers, and cleaner disposal.
    /// </summary>
    public sealed partial class HomePage : Page, IDisposable
    {
        private readonly SystemInfoService _systemInfoService = SystemInfoService.Shared;
        private const int TopProcessCount = 5;
        private readonly ProcessManagementService _processManagementService;
        private readonly DatabaseService _databaseService;
        private readonly ProcessDetailsDialogService _processDetailsDialogService;

        private readonly DispatcherTimer _uptimeTimer;
        private readonly DispatcherTimer _refreshTimer;

        private bool _disposed;
        private int _isDashboardLoading;

        /// <summary>
        /// Collection of top processes displayed in the dashboard.
        /// </summary>
        public ObservableCollection<TopProcessItem> TopProcesses { get; } = new();

        /// <summary>
        /// Page constructor. Initializes services, timers, and event handlers.
        /// </summary>
        public HomePage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Disabled;

            _processManagementService = new ProcessManagementService(ApplicationInfoService.Shared);
            _databaseService = DatabaseService.Instance;
            _processDetailsDialogService = new ProcessDetailsDialogService(_databaseService);

            TopProcessesListView.ItemsSource = TopProcesses;

            // Initialize uptime timer (1 second interval)
            _uptimeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _uptimeTimer.Tick += OnUptimeTimerTick;

            // Initialize refresh timer (5 seconds interval)
            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(Configuration.DefaultRefreshIntervalSeconds) };
            _refreshTimer.Tick += OnRefreshTimerTick;

            Unloaded += OnPageUnloaded;
            Loaded += OnPageLoaded;
            TopProcesses.CollectionChanged += TopProcesses_CollectionChanged;
        }

        /// <summary>
        /// Starts the dashboard updates when the page is loaded.
        /// </summary>
        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            OnUptimeTimerTick(null, e);
            _uptimeTimer.Start();
            _refreshTimer.Start();
            _ = LoadDashboardDataAsync();
            _ = LoadWindowsVersionAsync();
        }

        /// <summary>
        /// Loads Windows version asynchronously to avoid blocking the UI thread.
        /// </summary>
        private async Task LoadWindowsVersionAsync()
        {
            try
            {
                var windowsVersion = await Task.Run(GetWindowsVersionInfo);

                if (!_disposed)
                    WindowsVersionText.Text = windowsVersion;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading Windows version: {ex.Message}");

                if (!_disposed)
                    WindowsVersionText.Text = "Unknown";
            }
        }

        /// <summary>
        /// Reads Windows version information from the registry.
        /// Handles the Windows 10/11 build number distinction correctly.
        /// </summary>
        private string GetWindowsVersionInfo()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                if (key != null)
                {
                    var productName = key.GetValue("ProductName")?.ToString() ?? "Windows";
                    var displayVersion = key.GetValue("DisplayVersion")?.ToString();
                    var currentBuildStr = key.GetValue("CurrentBuild")?.ToString();
                    var releaseId = key.GetValue("ReleaseId")?.ToString();

                    // ProductName incorrectly reports "Windows 10" on Windows 11 machines.
                    // We reliably distinguish them using CurrentBuild (Windows 11 starts at build 22000).
                    bool isWindows11 = int.TryParse(currentBuildStr, out int buildNumber) && buildNumber >= 22000;
                    string windowsName = isWindows11 ? "Windows 11" : "Windows 10";

                    if (!string.IsNullOrWhiteSpace(displayVersion))
                        return $"{windowsName}\n{displayVersion}";

                    if (!string.IsNullOrWhiteSpace(releaseId))
                        return $"{windowsName}\n{releaseId}";

                    if (!string.IsNullOrWhiteSpace(currentBuildStr))
                        return $"{windowsName}\nBuild {currentBuildStr}";

                    return windowsName;
                }

                var osVersion = Environment.OSVersion;
                return $"Windows {osVersion.Version.Major}.{osVersion.Version.Minor}";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reading Windows version from registry: {ex.Message}");
                return Environment.OSVersion.VersionString;
            }
        }

        /// <summary>
        /// Loads icons for newly added top processes.
        /// </summary>
        private void TopProcesses_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_disposed) return;

            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
            {
                foreach (TopProcessItem item in e.NewItems)
                    _ = item.LoadIconAsync();
            }
        }

        /// <summary>
        /// Stops timers when the page is unloaded to save CPU cycles.
        /// </summary>
        private void OnPageUnloaded(object sender, RoutedEventArgs e)
        {
            _uptimeTimer.Stop();
            _refreshTimer.Stop();
        }

        /// <summary>
        /// Updates uptime text every second.
        /// </summary>
        private void OnUptimeTimerTick(object? sender, object e)
        {
            if (_disposed) return;

            var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
            UptimeText.Text = FormatUptime(uptime);
        }

        /// <summary>
        /// Refreshes dashboard data every few seconds.
        /// </summary>
        private async void OnRefreshTimerTick(object? sender, object e)
        {
            if (_disposed) return;

            try
            {
                await LoadDashboardDataAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in refresh timer: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads top processes and system information.
        /// Uses a concurrency flag to prevent overlapping refreshes.
        /// </summary>
        private async Task LoadDashboardDataAsync()
        {
            if (_disposed) return;

            if (Interlocked.Exchange(ref _isDashboardLoading, 1) == 1)
                return;

            try
            {
                var snapshot = await _systemInfoService.GetDashboardSnapshotAsync(TopProcessCount);
                if (_disposed) return;

                UpdateTopProcesses(snapshot.TopProcesses);
                TotalProcessesText.Text = snapshot.TotalProcesses.ToString();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading dashboard data: {ex.Message}");
            }
            finally
            {
                Volatile.Write(ref _isDashboardLoading, 0);
            }
        }

        /// <summary>Updates the ranking while retaining the rows and icons of processes still listed.</summary>
        private void UpdateTopProcesses(IReadOnlyList<SystemInfoService.ProcessUsage> rankedProcesses)
        {
            for (int index = TopProcesses.Count - 1; index >= 0; index--)
            {
                var item = TopProcesses[index];
                if (rankedProcesses.Any(sample => MatchesProcess(item, sample)))
                    continue;

                TopProcesses.RemoveAt(index);
                item.Dispose();
            }

            for (int index = 0; index < rankedProcesses.Count; index++)
            {
                var sample = rankedProcesses[index];
                int currentIndex = index;
                while (currentIndex < TopProcesses.Count && !MatchesProcess(TopProcesses[currentIndex], sample))
                    currentIndex++;

                if (currentIndex == TopProcesses.Count)
                {
                    var item = new TopProcessItem { ProcessId = sample.Id, ProcessName = sample.Name };
                    item.UpdateUsage(sample);
                    TopProcesses.Insert(index, item);
                    continue;
                }

                TopProcesses[currentIndex].UpdateUsage(sample);
                if (currentIndex != index)
                    TopProcesses.Move(currentIndex, index);
            }
        }

        private static bool MatchesProcess(TopProcessItem item, SystemInfoService.ProcessUsage sample) =>
            item.ProcessId == sample.Id && string.Equals(item.ProcessName, sample.Name, StringComparison.Ordinal);

        /// <summary>
        /// Formats uptime into a friendly string.
        /// </summary>
        private static string FormatUptime(TimeSpan ts)
        {
            if (ts.TotalDays >= 1)
                return $"{(int)ts.TotalDays}d {ts.Hours}h {ts.Minutes}m {ts.Seconds}s";

            if (ts.TotalHours >= 1)
                return $"{ts.Hours}h {ts.Minutes}m {ts.Seconds}s";

            return $"{ts.Minutes}m {ts.Seconds}s";
        }

        /// <summary>
        /// Manual refresh button click handler.
        /// </summary>
        private async void RefreshTopProcessesButton_Click(object sender, RoutedEventArgs e)
        {
            if (_disposed) return;

            try
            {
                await LoadDashboardDataAsync();
                NotificationService.Instance.ShowSuccess("Dashboard refreshed");
            }
            catch (Exception ex)
            {
                NotificationService.Instance.ShowError($"Refresh failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Opens details for a top process.
        /// </summary>
        private async void ViewDetailsTopProcessButton_Click(object sender, RoutedEventArgs e)
        {
            if (_disposed || _processDetailsDialogService == null) return;

            if (sender is Button button && button.DataContext is TopProcessItem topProcess)
            {
                try
                {
                    using var processItem = new ProcessItem
                    {
                        ProcessId = topProcess.ProcessId,
                        ProcessName = topProcess.ProcessName,
                        ProcessIcon = topProcess.ProcessIcon
                    };

                    if (processItem.ProcessIcon == null)
                    {
                        await processItem.LoadIconAsync();
                    }

                    await _processDetailsDialogService.ShowAsync(
                        processItem,
                        button.XamlRoot,
                        onScanRequested: ScanProcessFromDialogAsync);
                }
                catch (Exception ex)
                {
                    NotificationService.Instance.ShowError($"Unable to open process details: {ex.Message}");
                    Debug.WriteLine(ex);
                }
            }
        }

        /// <summary>
        /// Scans a process from the details dialog.
        /// </summary>
        private async Task ScanProcessFromDialogAsync(ProcessItem process, CancellationToken cancellationToken)
        {
            var virusScanService = VirusScanService.Shared;

            if (!virusScanService.IsConfigured)
            {
                NotificationService.Instance.ShowWarning("VirusTotal API key not configured.");
                throw new InvalidOperationException("VirusTotal API key not configured.");
            }

            process.VirusStatus = "Scanning...";
            var filePath = process.FilePath;

            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                using var runningProcess = _processManagementService.GetProcessById(process.ProcessId);
                if (runningProcess != null)
                {
                    filePath = _processManagementService.GetProcessPath(runningProcess);
                }
            }

            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                process.VirusStatus = "The executable path is unavailable.";
                NotificationService.Instance.ShowWarning(process.VirusStatus);
                return;
            }

            var vtResult = await virusScanService.ScanFileAsync(filePath, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            if (!vtResult.HasReport)
            {
                process.VirusStatus = vtResult.Summary;
                NotificationService.Instance.ShowWarning(vtResult.Summary);
                return;
            }

            process.VirusStatus = "Scanned";
            process.FilePath = filePath;
            process.ScanDetails = vtResult;
            process.IsScanned = true;

            await ScanHistoryService.Shared.SaveAsync(filePath, vtResult);
            if (!_disposed && !cancellationToken.IsCancellationRequested)
                NotificationService.Instance.ShowSuccess($"Scanned {process.ProcessName}.");
        }

        /// <summary>
        /// Releases all page resources and detaches event handlers.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            Unloaded -= OnPageUnloaded;
            Loaded -= OnPageLoaded;
            TopProcesses.CollectionChanged -= TopProcesses_CollectionChanged;

            _refreshTimer.Stop();
            _refreshTimer.Tick -= OnRefreshTimerTick;

            _uptimeTimer.Stop();
            _uptimeTimer.Tick -= OnUptimeTimerTick;

            var oldItems = TopProcesses.ToList();
            TopProcesses.Clear();

            foreach (var old in oldItems)
            {
                try
                {
                    old.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error disposing TopProcessItem: {ex.Message}");
                }
            }

            if (TopProcessesListView != null)
                TopProcessesListView.ItemsSource = null;

            _processManagementService?.Dispose();
            _processDetailsDialogService?.Dispose();

            DataContext = null;

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the page when navigating away.
        /// </summary>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            Dispose();
        }
    }

    /// <summary>
    /// Model for top process items shown on the dashboard.
    /// </summary>
    public class TopProcessItem : INotifyPropertyChanged, IDisposable
    {
        private int _processId;
        private string _processName = string.Empty;
        private double _cpuUsageValue;
        private long _memoryUsageValue;
        private string _cpuUsage = string.Empty;
        private string _memoryUsage = string.Empty;
        private Microsoft.UI.Xaml.Media.Imaging.BitmapImage? _processIcon;
        private bool _disposed;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }

        public int ProcessId
        {
            get => _processId;
            set => SetProperty(ref _processId, value);
        }

        public string ProcessName
        {
            get => _processName;
            set => SetProperty(ref _processName, value);
        }

        public double CpuUsageValue
        {
            get => _cpuUsageValue;
            set => SetProperty(ref _cpuUsageValue, value);
        }

        public long MemoryUsageValue
        {
            get => _memoryUsageValue;
            set => SetProperty(ref _memoryUsageValue, value);
        }

        public string CpuUsage
        {
            get => _cpuUsage;
            set => SetProperty(ref _cpuUsage, value);
        }

        public string MemoryUsage
        {
            get => _memoryUsage;
            set => SetProperty(ref _memoryUsage, value);
        }

        public Microsoft.UI.Xaml.Media.Imaging.BitmapImage? ProcessIcon
        {
            get => _processIcon;
            set => SetProperty(ref _processIcon, value);
        }

        /// <summary>Applies a new sample and notifies bindings when displayed values change.</summary>
        public void UpdateUsage(SystemInfoService.ProcessUsage sample)
        {
            CpuUsageValue = sample.Cpu;
            CpuUsage = $"{sample.Cpu:F1}%";
            MemoryUsageValue = sample.Memory;
            MemoryUsage = sample.FormattedMemory;
        }

        /// <summary>
        /// Loads the process icon if it has not been loaded yet.
        /// </summary>
        public async Task LoadIconAsync()
        {
            if (_disposed || ProcessIcon != null) return;

            var icon = await ProcessIconService.GetIconForProcessAsync(_processId);
            if (!_disposed)
                ProcessIcon = icon;
        }

        /// <summary>
        /// Releases item resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            _processIcon = null;
            _disposed = true;

            GC.SuppressFinalize(this);
        }
    }

}
