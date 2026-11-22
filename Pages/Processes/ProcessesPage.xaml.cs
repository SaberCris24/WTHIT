using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
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
using WTHIT.Core;
using WTHIT.Models;
using WTHIT.Services;

namespace WTHIT.Pages.Processes
{
    /// <summary>
    /// Processes page displaying all running processes with grouping and filtering.
    /// Optimized for faster UI updates, lower RAM usage, safer icon loading,
    /// correct notification initialization, and better cleanup when navigating away.
    /// </summary>
    public sealed partial class ProcessesPage : Page, IDisposable, INotifyPropertyChanged
    {
        private readonly ApplicationInfoService _applicationInfoService = ApplicationInfoService.Shared;
        private readonly ProcessSearchService _processSearchService = ProcessSearchService.Shared;
        private readonly ProcessSelectionService _processSelectionService = ProcessSelectionService.Shared;

        private DatabaseService? _databaseService;
        private ProcessDetailsDialogService? _processDetailsDialogService;
        private ProcessManagementService? _processManagementService;

        private ObservableCollection<ProcessItem>? _processes;
        private List<ProcessItem>? _allProcesses;

        private CancellationTokenSource? _loadCancellationTokenSource;
        private CancellationTokenSource? _scanCancellationTokenSource;

        private readonly CancellationTokenSource _iconCts = new();

        private readonly SemaphoreSlim _loadProcessesLock = new(1, 1);

        private readonly SemaphoreSlim _iconLoadSemaphore = new(2);

        private readonly SemaphoreSlim _scanSemaphore = new(3);

        private readonly HashSet<int> _expandedGroups = new();
        private readonly Dictionary<int, List<ProcessItem>> _groupChildren = new();

        private int _displayedFlatCount;
        private bool _isProcessSelected;
        private bool _disposed;
        private bool _isScanning;
        private bool _servicesInitialized;
        private string? _activeFilter;

        /// <summary>
        /// Indicates whether at least one process is selected.
        /// </summary>
        public bool IsProcessSelected
        {
            get => _isProcessSelected;
            set
            {
                if (_isProcessSelected != value)
                {
                    _isProcessSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Process count text shown above the list.
        /// </summary>
        public string ProcessCountText
        {
            get
            {
                int count = _displayedFlatCount;
                return count == 1 ? "1 process" : $"{count} processes";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        /// <summary>
        /// Page constructor. Initializes UI components and event handlers.
        /// </summary>
        public ProcessesPage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Disabled;

            _processes = new ObservableCollection<ProcessItem>();
            _allProcesses = new List<ProcessItem>();

            ProcessListView.ItemsSource = _processes;
            DataContext = this;

            Loaded += OnPageLoaded;
            Unloaded += OnPageUnloaded;
        }

        private void RegisterCollectionChanged()
        {
            if (_processes != null)
            {
                _processes.CollectionChanged -= Processes_CollectionChanged;
                _processes.CollectionChanged += Processes_CollectionChanged;
            }
        }

        private void Processes_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_disposed) return;

            UpdateProcessCountAndEmptyState();
        }

        private void Process_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ProcessItem.IsSelected))
                return;

            // Cascade selection from a group header to all child instances.
            if (sender is ProcessItem groupItem &&
                groupItem.InstanceCount > 1 &&
                !groupItem.IsChildRow &&
                _groupChildren.TryGetValue(groupItem.ProcessId, out var instances))
            {
                foreach (var inst in instances)
                {
                    // Temporarily detach to avoid recursive notifications and redundant work.
                    inst.PropertyChanged -= Process_PropertyChanged;
                    inst.IsSelected = groupItem.IsSelected;
                    inst.PropertyChanged += Process_PropertyChanged;
                }
            }

            UpdateSelectionState();
        }

        private void UpdateProcessCountAndEmptyState()
        {
            OnPropertyChanged(nameof(ProcessCountText));

            if (EmptyStateText != null)
            {
                EmptyStateText.Visibility = (_processes?.Count == 0)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private async Task InitializeServicesAsync()
        {
            if (_servicesInitialized) return;

            try
            {
                _databaseService = DatabaseService.Instance;
                _processDetailsDialogService = new ProcessDetailsDialogService(_databaseService);
                _processManagementService = new ProcessManagementService(_applicationInfoService);

                await _databaseService.InitializeAsync();

                _servicesInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing services: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Event handler triggered when the page is loaded into the visual tree.
        /// </summary>
        private async void OnPageLoaded(object sender, RoutedEventArgs e)
        {

            try
            {
                await InitializeServicesAsync();
            }
            catch (Exception ex)
            {
                if (!_disposed)
                    NotificationService.Instance.ShowError($"Failed to initialize services: {ex.Message}");
                return;
            }

            if (_disposed) return;

            RegisterCollectionChanged();

            try
            {
                await LoadProcessesAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnPageLoaded: {ex.Message}");
                NotificationService.Instance.ShowError("Failed to load processes");
            }
        }

        private void OnPageUnloaded(object sender, RoutedEventArgs e)
        {
            _loadCancellationTokenSource?.Cancel();
            _scanCancellationTokenSource?.Cancel();
            CleanupProcessHandlers();
        }

        private void ProcessListView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (!_disposed && !args.InRecycleQueue && args.Item is ProcessItem { ProcessIcon: null })
                args.RegisterUpdateCallback(LoadContainerIcon);
        }

        private async void LoadContainerIcon(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (_disposed || args.InRecycleQueue || args.Item is not ProcessItem item || item.ProcessIcon != null)
                return;

            var container = args.ItemContainer;
            var cancellationToken = _iconCts.Token;
            try
            {
                await _iconLoadSemaphore.WaitAsync(cancellationToken);
                try
                {
                    if (!cancellationToken.IsCancellationRequested && ReferenceEquals(sender.ItemFromContainer(container), item))
                        await item.LoadIconAsync();
                }
                finally
                {
                    _iconLoadSemaphore.Release();
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load the icon for {item.ProcessName}: {ex.Message}");
            }
        }

        private void CleanupProcessHandlers()
        {
            if (_allProcesses != null)
            {
                foreach (var process in _allProcesses)
                {
                    try
                    {
                        process.PropertyChanged -= Process_PropertyChanged;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }
                }
            }

            if (_processes != null)
            {
                foreach (var process in _processes)
                {
                    try
                    {
                        process.PropertyChanged -= Process_PropertyChanged;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }
                }
            }
        }

        private void DetachHandlersFromAllProcesses()
        {
            if (_allProcesses == null) return;

            foreach (var process in _allProcesses)
            {
                try
                {
                    process.PropertyChanged -= Process_PropertyChanged;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
        }

        private async Task LoadProcessesAsync()
        {
            if (_disposed) return;

            try
            {
                if (!await _loadProcessesLock.WaitAsync(0))
                    return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            _loadCancellationTokenSource?.Cancel();
            _loadCancellationTokenSource?.Dispose();
            _loadCancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = _loadCancellationTokenSource.Token;

            try
            {
                if (LoadingRing != null)
                    LoadingRing.IsActive = true;

                var processManagementService = _processManagementService;
                if (processManagementService == null)
                    return;

                var fetchedProcesses = await Task.Run(() =>
                {
                    var processes = processManagementService.GetAllProcesses();
                    ScanHistoryService.Shared.RestoreForProcesses(processes, processManagementService, cancellationToken);
                    return processes;
                }, cancellationToken);

                if (_processes == null || fetchedProcesses == null || cancellationToken.IsCancellationRequested)
                    return;

                DetachHandlersFromAllProcesses();

                _allProcesses = fetchedProcesses
                    .OrderBy(p => p.ApplicationRelated ?? string.Empty)
                    .ToList();

                UpdateDisplayedProcesses(_allProcesses);

                if (!cancellationToken.IsCancellationRequested)
                {
                    UpdateSelectionState();
                    UpdateSelectAllCheckBox();
                }
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Process load cancelled");
            }
            catch (Exception ex)
            {
                if (!_disposed)
                {
                    NotificationService.Instance.ShowError($"Error loading processes: {ex.Message}");
                }

                Debug.WriteLine($"Error loading processes: {ex}");
            }
            finally
            {
                if (!_disposed && LoadingRing != null)
                    LoadingRing.IsActive = false;

                try
                {
                    _loadProcessesLock.Release();
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }

        private List<ProcessItem> BuildGroupedRows(IEnumerable<ProcessItem> flat)
        {
            var source = flat.ToList();
            _displayedFlatCount = source.Count;

            var rows = new List<ProcessItem>(source.Count);
            _groupChildren.Clear();

            var groups = new Dictionary<string, List<ProcessItem>>(StringComparer.OrdinalIgnoreCase);
            var order = new List<string>();

            foreach (var p in source)
            {
                var key = p.ProcessName ?? string.Empty;

                if (!groups.TryGetValue(key, out var list))
                {
                    list = new List<ProcessItem>();
                    groups[key] = list;
                    order.Add(key);
                }

                list.Add(p);
            }

            foreach (var key in order)
            {
                var instances = groups[key];
                instances.Sort((a, b) => a.ProcessId.CompareTo(b.ProcessId));

                var primary = instances[0];

                primary.IsChildRow = false;
                primary.InstanceCount = instances.Count;
                primary.GroupCaption = instances.Count > 1 ? $"{instances.Count} instances" : "Process";
                primary.IsGroupExpanded = _expandedGroups.Contains(primary.ProcessId);

                rows.Add(primary);

                if (instances.Count > 1)
                {
                    var children = instances.Skip(1).ToList();
                    _groupChildren[primary.ProcessId] = children;

                    foreach (var c in children)
                    {
                        c.IsChildRow = true;
                        c.InstanceCount = 1;
                        c.GroupCaption = "Instance";
                    }

                    if (primary.IsGroupExpanded)
                        rows.AddRange(children);
                }
            }

            return rows;
        }

        private void ToggleGroup_Click(object sender, RoutedEventArgs e)
        {
            if (_processes == null) return;

            if (sender is Button btn &&
                btn.DataContext is ProcessItem primary &&
                primary.InstanceCount > 1)
            {
                var idx = _processes.IndexOf(primary);
                if (idx < 0 || !_groupChildren.TryGetValue(primary.ProcessId, out var children))
                    return;

                if (primary.IsGroupExpanded)
                {
                    primary.IsGroupExpanded = false;
                    _expandedGroups.Remove(primary.ProcessId);

                    foreach (var c in children)
                    {
                        c.PropertyChanged -= Process_PropertyChanged;
                        _processes.Remove(c);
                    }
                }
                else
                {
                    primary.IsGroupExpanded = true;
                    _expandedGroups.Add(primary.ProcessId);

                    for (int i = 0; i < children.Count; i++)
                    {
                        var c = children[i];

                        c.PropertyChanged -= Process_PropertyChanged;
                        c.PropertyChanged += Process_PropertyChanged;

                        _processes.Insert(idx + 1 + i, c);
                    }
                }

                UpdateProcessCountAndEmptyState();
            }
        }

        private void UpdateDisplayedProcesses(IEnumerable<ProcessItem> newSource)
        {
            if (_disposed) return;

            var groupedRows = BuildGroupedRows(newSource);
            var newCollection = new ObservableCollection<ProcessItem>(groupedRows);

            // Detach old handlers before replacing the collection.
            if (_processes != null)
            {
                _processes.CollectionChanged -= Processes_CollectionChanged;

                foreach (var p in _processes)
                {
                    try
                    {
                        p.PropertyChanged -= Process_PropertyChanged;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }
                }
            }

            _processes = newCollection;

            if (ProcessListView != null)
                ProcessListView.ItemsSource = _processes;

            RegisterCollectionChanged();

            foreach (var p in _processes)
            {
                p.PropertyChanged -= Process_PropertyChanged;
                p.PropertyChanged += Process_PropertyChanged;
            }

            UpdateProcessCountAndEmptyState();
        }

        private void UpdateSelectionState()
        {
            if (_processSelectionService != null && _processes != null)
            {
                IsProcessSelected = _processSelectionService.HasSelectedProcesses(_processes);
            }

            UpdateSelectAllCheckBox();
        }

        private void UpdateSelectAllCheckBox()
        {
            if (SelectAllCheckBox == null || _processes == null) return;

            if (_processes.Count == 0)
            {
                SelectAllCheckBox.IsChecked = false;
                return;
            }

            var allSelected = _processes.All(p => p.IsSelected);
            var anySelected = _processes.Any(p => p.IsSelected);

            SelectAllCheckBox.IsChecked = allSelected ? true : (anySelected ? null : false);
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e) => _ = LoadProcessesAsync();

        private async void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            if (_disposed) return;

            if (_isScanning)
            {
                NotificationService.Instance.ShowWarning("A scan is already in progress.");
                return;
            }

            var virusScanService = VirusScanService.Shared;

            if (!virusScanService.IsConfigured)
            {
                NotificationService.Instance.ShowWarning("VirusTotal API key not configured.");
                return;
            }

            if (_processSelectionService == null || _allProcesses == null) return;

            var selectedProcesses = _processSelectionService.GetSelectedProcesses(_allProcesses);
            if (!selectedProcesses.Any()) return;

            _scanCancellationTokenSource?.Cancel();
            _scanCancellationTokenSource?.Dispose();
            _scanCancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = _scanCancellationTokenSource.Token;

            _isScanning = true;
            ScanButton.IsEnabled = false;

            try
            {
                var lookups = new Dictionary<string, Task<VirusTotalScanResult>>(StringComparer.OrdinalIgnoreCase);
                var failures = await Task.WhenAll(selectedProcesses.Select(process =>
                    ScanSingleProcessAsync(process, cancellationToken, lookups)));

                if (!cancellationToken.IsCancellationRequested)
                {
                    int reportCount = failures.Count(failure => failure == null);
                    if (reportCount == failures.Length)
                        NotificationService.Instance.ShowSuccess($"Retrieved {reportCount} VirusTotal report(s).");
                    else
                        NotificationService.Instance.ShowWarning(
                            $"Retrieved {reportCount} of {failures.Length} reports. {failures.First(failure => failure != null)}");
                }
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Scanning cancelled.");
            }
            finally
            {
                _isScanning = false;

                if (!_disposed)
                    ScanButton.IsEnabled = IsProcessSelected;
            }
        }

        /// <summary>Looks up a process report and returns a message when no report can be retrieved.</summary>
        private async Task<string?> ScanSingleProcessAsync(ProcessItem process, CancellationToken cancellationToken,
            Dictionary<string, Task<VirusTotalScanResult>>? lookups = null)
        {
            await _scanSemaphore.WaitAsync(cancellationToken);
            ScanResultItem? scanResult = null;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                scanResult = new ScanResultItem { ProcessName = process.ProcessName, ScanResult = "Scanning..." };
                ScanResultsManager.AddScanResult(scanResult);
                process.VirusStatus = "Scanning...";

                var management = _processManagementService;
                using var runningProcess = management?.GetProcessById(process.ProcessId);
                if (runningProcess == null)
                    return RecordFailure("Process not found.");

                var path = management!.GetProcessPath(runningProcess);
                if (string.IsNullOrWhiteSpace(path))
                    return RecordFailure("The executable path is unavailable.");

                Task<VirusTotalScanResult> lookup;
                if (lookups == null || !lookups.TryGetValue(path, out lookup!))
                {
                    lookup = LookupAndSaveReportAsync(path, cancellationToken);
                    lookups?.Add(path, lookup);
                }

                var report = await lookup;
                cancellationToken.ThrowIfCancellationRequested();
                if (!report.HasReport)
                    return RecordFailure(report.Summary);

                process.VirusStatus = "Scanned";
                process.FilePath = path;
                process.ScanDetails = report;
                process.IsScanned = true;

                scanResult.ScanResult = report.Summary;
                scanResult.ScanDetails = report;
                scanResult.FilePath = path;
                scanResult.IsScanned = true;
                return null;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                RecordFailure("Scan cancelled.");
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return RecordFailure("The file report could not be retrieved.");
            }
            finally
            {
                _scanSemaphore.Release();
            }

            string RecordFailure(string message)
            {
                if (scanResult != null)
                    scanResult.ScanResult = message;
                if (!_disposed)
                    process.VirusStatus = message;

                return message;
            }
        }

        /// <summary>Stores only completed reports and shares the request for matching paths within a batch.</summary>
        private static async Task<VirusTotalScanResult> LookupAndSaveReportAsync(string path, CancellationToken cancellationToken)
        {
            var report = await VirusScanService.Shared.ScanFileAsync(path, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (report.HasReport)
                await ScanHistoryService.Shared.SaveAsync(path, report);

            return report;
        }

        private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (_disposed || _processes == null || _processSearchService == null || _allProcesses == null)
                return;

            if (string.IsNullOrWhiteSpace(args.QueryText))
            {
                UpdateDisplayedProcesses(_allProcesses);
                return;
            }

            var searchResults = _processSearchService.SearchProcesses(_allProcesses, args.QueryText).ToList();
            UpdateDisplayedProcesses(searchResults);
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (_disposed || _processSearchService == null || _processes == null || _allProcesses == null)
                return;

            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                if (string.IsNullOrWhiteSpace(sender.Text))
                {
                    UpdateDisplayedProcesses(_allProcesses);
                    return;
                }

                var suggestions = _processSearchService.GetSearchSuggestions(_allProcesses, sender.Text);
                sender.ItemsSource = suggestions;
            }
        }

        private void SearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (_disposed) return;

            if (args.SelectedItem is string processName)
                sender.Text = processName;
        }

        private void FilterByName_Click(object sender, RoutedEventArgs e)
        {
            if (_disposed || _processes == null) return;

            if (_activeFilter == "name")
            {
                _activeFilter = null;
                FilterByNameItem.IsChecked = false;
            }
            else
            {
                _activeFilter = "name";
                FilterByNameItem.IsChecked = true;
                FilterByPidItem.IsChecked = false;
            }

            ApplyFilter();
        }

        private void FilterByPid_Click(object sender, RoutedEventArgs e)
        {
            if (_disposed || _processes == null) return;

            if (_activeFilter == "pid")
            {
                _activeFilter = null;
                FilterByPidItem.IsChecked = false;
            }
            else
            {
                _activeFilter = "pid";
                FilterByPidItem.IsChecked = true;
                FilterByNameItem.IsChecked = false;
            }

            ApplyFilter();
        }

        private void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            if (_disposed) return;

            _activeFilter = null;
            FilterByNameItem.IsChecked = false;
            FilterByPidItem.IsChecked = false;
            SearchBox.Text = string.Empty;

            UpdateDisplayedProcesses(_allProcesses ?? new List<ProcessItem>());
        }

        private void ApplyFilter()
        {
            if (_disposed || _allProcesses == null || _processes == null) return;

            var filteredProcesses = _activeFilter switch
            {
                "name" => _allProcesses.OrderBy(p => p.ProcessName).ToList(),
                "pid" => _allProcesses.OrderBy(p => p.ProcessId).ToList(),
                _ => _allProcesses.ToList()
            };

            UpdateDisplayedProcesses(filteredProcesses);
        }

        private void SelectAllCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (_disposed || _processes == null) return;

            foreach (var process in _processes)
                process.IsSelected = true;

            UpdateSelectionState();
        }

        private void SelectAllCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_disposed || _processes == null) return;

            foreach (var process in _processes)
                process.IsSelected = false;

            UpdateSelectionState();
        }

        private async void VirusStatusButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ProcessItem process)
            {
                if (_processDetailsDialogService != null)
                {
                    await _processDetailsDialogService.ShowAsync(
                        process,
                        XamlRoot,
                        onScanRequested: ScanProcessFromDialogAsync);
                }
            }
        }

        private async void ProcessListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (_disposed || e.ClickedItem is not ProcessItem process || _processDetailsDialogService == null)
                return;

            try
            {
                await _processDetailsDialogService.ShowAsync(
                    process,
                    XamlRoot,
                    onScanRequested: ScanProcessFromDialogAsync);
            }
            catch (Exception ex)
            {
                NotificationService.Instance.ShowError($"Unable to open process details: {ex.Message}");
                Debug.WriteLine(ex);
            }
        }

        private async Task ScanProcessFromDialogAsync(ProcessItem process, CancellationToken cancellationToken)
        {
            var virusScanService = VirusScanService.Shared;

            if (!virusScanService.IsConfigured)
            {
                NotificationService.Instance.ShowWarning("VirusTotal API key not configured.");
                throw new InvalidOperationException("VirusTotal API key not configured.");
            }

            var failure = await ScanSingleProcessAsync(process, cancellationToken);
            if (failure != null && !_disposed && !cancellationToken.IsCancellationRequested)
                NotificationService.Instance.ShowWarning(failure);
        }

        private static void DisposeProcessItems(IEnumerable<ProcessItem>? items)
        {
            if (items == null) return;

            foreach (var item in items)
            {
                try
                {
                    if (item is IDisposable disposable)
                        disposable.Dispose();
                }
                catch
                {
                    // Ignore dispose errors during page cleanup.
                }
            }
        }

        /// <summary>
        /// Releases all page resources and detaches event handlers.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _loadCancellationTokenSource?.Cancel();
            _loadCancellationTokenSource?.Dispose();
            _loadCancellationTokenSource = null;

            _scanCancellationTokenSource?.Cancel();
            _scanCancellationTokenSource?.Dispose();
            _scanCancellationTokenSource = null;

            _iconCts.Cancel();
            _iconCts.Dispose();


            if (_processes != null)
            {
                _processes.CollectionChanged -= Processes_CollectionChanged;
            }

            Loaded -= OnPageLoaded;
            Unloaded -= OnPageUnloaded;

            CleanupProcessHandlers();

            if (ProcessListView != null)
                ProcessListView.ItemsSource = null;

            _processDetailsDialogService?.Dispose();
            _processDetailsDialogService = null;

            _processManagementService?.Dispose();
            _processManagementService = null;

            DisposeProcessItems(_processes);
            DisposeProcessItems(_allProcesses);

            if (_processes != null)
            {
                _processes.Clear();
                _processes = null;
            }

            if (_allProcesses != null)
            {
                _allProcesses.Clear();
                _allProcesses = null;
            }

            _groupChildren.Clear();
            _expandedGroups.Clear();

            _databaseService = null;
            DataContext = null;

            GC.SuppressFinalize(this);
        }

        /// <summary>Releases page resources after navigation completes.</summary>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            Dispose();
        }
    }
}
