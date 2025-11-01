using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Plantilla.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Plantilla.Pages.Home
{
    public sealed partial class HomePage : Page, IDisposable
    {
        private readonly ISystemInfoService _systemInfoService;
        
        /// <summary>
        /// Fast timer for uptime (stopwatch)
        /// </summary>
        private readonly DispatcherTimer _uptimeTimer;
        
        /// <summary>
        /// Slow timer for processes
        /// </summary>
        private readonly DispatcherTimer _refreshTimer;
        
        private readonly IVirusScanService _virusScanService;
        private readonly IProcessManagementService _processManagementService;
        private readonly INotificationService _notificationService;

        private ObservableCollection<TopProcessItem> TopProcesses { get; set; }
        public ObservableCollection<ScanResultItem> ScanResults => ScanResultsManager.ScanResults;

        private bool _isCpuSortAscending = false;
        private bool _isRamSortAscending = false;

        public HomePage()
        {
            this.InitializeComponent();
            
            _systemInfoService = new SystemInfoService();
            _virusScanService = new VirusScanService("1234");
            _processManagementService = new ProcessManagementService(new ApplicationInfoService());
            _notificationService = new NotificationService();
            
            TopProcesses = new ObservableCollection<TopProcessItem>();
            
            TopProcessesListView.ItemsSource = TopProcesses;
            TopProcessesListViewNarrow.ItemsSource = TopProcesses;

            ScanResultsListView.ItemsSource = ScanResults;
            ScanResultsListViewNarrow.ItemsSource = ScanResults;
            
            _notificationService.Initialize(StatusInfoBar);
            
            /// <summary>
            /// Initialize the stopwatch for real uptime
            /// </summary>
            _uptimeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _uptimeTimer.Tick += UptimeTimer_Tick;
            _uptimeTimer.Start();

            // Load initial process data
            _ = LoadDashboardDataAsync();
            
            // Configure the slow timer for processes
            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _refreshTimer.Tick += RefreshTimer_Tick;
            _refreshTimer.Start();

            UpdateCleanAllButtonVisibility();
        }

        /// <summary>
        /// Handler for the fast timer (stopwatch with real uptime)
        /// </summary>
        private void UptimeTimer_Tick(object? sender, object e)
        {
            // Gets the real system uptime and formats it
            var uptimeMilliseconds = Environment.TickCount64;
            var ts = TimeSpan.FromMilliseconds(uptimeMilliseconds);
            UptimeText.Text = FormatUptime(ts);
        }

        /// <summary>
        /// Handler for the slow timer (processes)
        /// </summary>
        private async void RefreshTimer_Tick(object? sender, object e)
        {
            await LoadDashboardDataAsync();
        }

        private async Task LoadDashboardDataAsync()
        {
            try
            {
                // Load the top 5 processes
                var topProcesses = await _systemInfoService.GetTopProcessesByResourceAsync(5);
                
                TopProcesses.Clear();
                foreach (var process in topProcesses)
                {
                    TopProcesses.Add(process);
                }

                // Load system information (only the number of processes)
                var systemInfo = await _systemInfoService.GetSystemInfoAsync();
                TotalProcessesText.Text = systemInfo.TotalProcesses.ToString();
                
                /// <summary>
                /// Uptime is no longer updated here, the stopwatch does it
                /// </summary>
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading dashboard data: {ex.Message}");
            }
        }

        /// <summary>
        /// Method to format the real uptime
        /// </summary>
        private string FormatUptime(TimeSpan ts)
        {
            if (ts.TotalDays >= 1)
            {
                return $"{(int)ts.TotalDays}d {ts.Hours}h {ts.Minutes}m {ts.Seconds}s";
            }
            else if (ts.TotalHours >= 1)
            {
                return $"{ts.Hours}h {ts.Minutes}m {ts.Seconds}s";
            }
            else
            {
                return $"{ts.Minutes}m {ts.Seconds}s";
            }
        }

        private async void RefreshTopProcessesButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadDashboardDataAsync();
        }

        private void SortByCpu_Click(object sender, RoutedEventArgs e)
        {
            _isCpuSortAscending = !_isCpuSortAscending;
            var sortedList = _isCpuSortAscending 
                ? TopProcesses.OrderBy(p => p.CpuUsageValue).ToList()
                : TopProcesses.OrderByDescending(p => p.CpuUsageValue).ToList();

            TopProcesses.Clear();
            foreach (var item in sortedList)
            {
                TopProcesses.Add(item);
            }
        }

        private void SortByRam_Click(object sender, RoutedEventArgs e)
        {
            _isRamSortAscending = !_isRamSortAscending;
            var sortedList = _isRamSortAscending 
                ? TopProcesses.OrderBy(p => p.MemoryUsageValue).ToList()
                : TopProcesses.OrderByDescending(p => p.MemoryUsageValue).ToList();

            TopProcesses.Clear();
            foreach (var item in sortedList)
            {
                TopProcesses.Add(item);
            }
        }

        private async void ScanTopProcessButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is TopProcessItem process)
            {
                var scanResult = new ScanResultItem { ProcessName = process.ProcessName, ScanResult = "Scanning..." };
                ScanResultsManager.AddScanResult(scanResult);
                UpdateCleanAllButtonVisibility();

                try
                {
                    var runningProcess = _processManagementService.GetProcessById(process.ProcessId);
                    if (runningProcess != null)
                    {
                        var path = _processManagementService.GetProcessPath(runningProcess);
                        if (!string.IsNullOrEmpty(path) && File.Exists(path))
                        {
                            scanResult.ScanResult = await _virusScanService.GetVirusTotalReportAsync(path);
                        }
                        else
                        {
                            scanResult.ScanResult = "No file path";
                        }
                    }
                    else
                    {
                        scanResult.ScanResult = "Process not found";
                    }
                }
                catch (Exception ex)
                {
                    scanResult.ScanResult = $"Error: {ex.Message}";
                }

                _notificationService.ShowSuccess($"Scanned {process.ProcessName}.");
            }
        }

        private void CleanScanResult_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ScanResultItem result)
            {
                ScanResults.Remove(result);
                UpdateCleanAllButtonVisibility();
            }
        }

        private void CleanAllScanResults_Click(object sender, RoutedEventArgs e)
        {
            ScanResults.Clear();
            UpdateCleanAllButtonVisibility();
        }

        private void UpdateCleanAllButtonVisibility()
        {
            var isVisible = ScanResults.Count > 1;
            CleanAllButton.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            CleanAllButtonNarrow.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        public void Dispose()
        {
            _refreshTimer?.Stop();
            _uptimeTimer?.Stop();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            Dispose();
        }
    }

    public class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string? propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value)) return false;
            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    public class TopProcessItem : ObservableObject
    {
        private int _processId;
        private string _processName = string.Empty;
        private double _cpuUsageValue;
        private string _cpuUsage = string.Empty;
        private long _memoryUsageValue;
        private string _memoryUsage = string.Empty;

        public int ProcessId { get => _processId; set => SetProperty(ref _processId, value); }
        public string ProcessName { get => _processName; set => SetProperty(ref _processName, value); }
        public double CpuUsageValue { get => _cpuUsageValue; set => SetProperty(ref _cpuUsageValue, value); }
        public long MemoryUsageValue { get => _memoryUsageValue; set => SetProperty(ref _memoryUsageValue, value); }
        public string CpuUsage { get => _cpuUsage; set => SetProperty(ref _cpuUsage, value); }
        public string MemoryUsage { get => _memoryUsage; set => SetProperty(ref _memoryUsage, value); }
    }

    public class ScanResultItem : ObservableObject
    {
        private string _scanResult = string.Empty;
        public string ProcessName { get; set; } = string.Empty;

        public string ScanResult
        {
            get => _scanResult;
            set => SetProperty(ref _scanResult, value);
        }
    }

    public static class ScanResultsManager
    {
        public static ObservableCollection<ScanResultItem> ScanResults { get; } = new ObservableCollection<ScanResultItem>();
        private const int MaxScanCount = 10;

        public static void AddScanResult(ScanResultItem result)
        {
            ScanResults.Insert(0, result);
            while (ScanResults.Count > MaxScanCount)
            {
                ScanResults.RemoveAt(ScanResults.Count - 1);
            }
        }
    }

    public class SystemInfoItem
    {
        public int TotalProcesses { get; set; }
        /// <summary>
        /// The Uptime property is no longer needed here
        /// </summary>
    }
}