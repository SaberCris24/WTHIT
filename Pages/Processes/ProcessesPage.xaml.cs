using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Plantilla.Models;
using System.ComponentModel;
using System.IO;
using System.Collections.Generic;
using Plantilla.Services;
using Plantilla.Pages.Home;

namespace Plantilla.Pages.Processes
{
    /// <summary>
    /// Main page that displays and manages system processes
    /// </summary>
    public sealed partial class ProcessesPage : Page, INotifyPropertyChanged
    {
        // Observable collection of processes displayed in the UI
        private ObservableCollection<ProcessItem> Processes { get; set; }
        
        // State variables
        private bool _isProcessSelected;
        private List<ProcessItem> allProcesses;
        private static bool _processesLoaded = false;
        
        // Injected services
        private readonly DatabaseService _databaseService;
        private readonly IVirusScanService _virusScanService;
        private readonly IApplicationInfoService _applicationInfoService;
        private readonly IProcessSearchService _processSearchService;
        private readonly IProcessDetailsDialogService _processDetailsDialogService;
        private readonly INotificationService _notificationService;
        private readonly IProcessManagementService _processManagementService;
        private readonly IProcessSelectionService _processSelectionService;
        private readonly IUISortingService _uiSortingService;

        /// <summary>
        /// Property that indicates if any process is selected
        /// Notifies the UI when its value changes
        /// </summary>
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

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Page constructor
        /// Initializes the components and required services
        /// </summary>
        public ProcessesPage()
        {
            // Initialize UI components
            this.InitializeComponent();
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
            Processes = new ObservableCollection<ProcessItem>();
            ProcessListView.ItemsSource = Processes;

            // Initialize services
            _databaseService = new DatabaseService();
            _virusScanService = new VirusScanService("8451868b2c4776276cc141e12591dbf3776b646dc60635fd6ff7f567f60176c4"); // Replace with your real API key
            _applicationInfoService = new ApplicationInfoService();
            _processSearchService = new ProcessSearchService();
            _processDetailsDialogService = new ProcessDetailsDialogService(_virusScanService, _databaseService);
            _notificationService = new NotificationService();
            _processManagementService = new ProcessManagementService(_applicationInfoService);
            _processSelectionService = new ProcessSelectionService();
            _uiSortingService = new UISortingService();
            
            // Initialize notification service
            _notificationService.Initialize(StatusInfoBar);

            // Initialize database
            _ = _databaseService.InitializeAsync();

            // Load initial processes
            if (allProcesses == null)
            {
                allProcesses = new List<ProcessItem>();
                LoadProcessesToAll();
            }
            else
            {
                RefreshProcessesFromAll();
            }

            // Configure Loaded event
            this.Loaded += (s, e) =>
            {
                if (!_processesLoaded)
                {
                    LoadProcesses();
                    _processesLoaded = true;
                }
            };
        }

        /// <summary>
        /// Loads all processes into the allProcesses list
        /// </summary>
        private void LoadProcessesToAll()
        {
            allProcesses.Clear();
            allProcesses.AddRange(_processManagementService.GetAllProcesses());
            RefreshProcessesFromAll();
        }

        /// <summary>
        /// Updates the observable collection from allProcesses
        /// </summary>
        private void RefreshProcessesFromAll()
        {
            Processes.Clear();
            foreach (var item in allProcesses)
            {
                Processes.Add(item);
            }
        }

        /// <summary>
        /// Loads system processes into the observable collection
        /// </summary>
        private void LoadProcesses()
        {
            try
            {
                LoadingRing.IsActive = true;
                Processes.Clear();

                var processes = _processManagementService.GetAllProcesses();
                foreach (var process in processes)
                {
                    Processes.Add(process);
                }

                UpdateSelectionState();
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Error loading processes: {ex.Message}");
            }
            finally
            {
                LoadingRing.IsActive = false;
            }
        }

        /// <summary>
        /// Updates the selection state of processes
        /// </summary>
        private void UpdateSelectionState()
        {
            IsProcessSelected = _processSelectionService.HasSelectedProcesses(Processes);
        }

        /// <summary>
        /// Click event handler for the Refresh button
        /// </summary>
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadProcesses();
        }

        /// <summary>
        /// Click event handler for the Scan button
        /// Scans the selected processes for viruses
        /// </summary>
        private async void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedProcesses = _processSelectionService.GetSelectedProcesses(Processes);
            if (selectedProcesses.Any())
            {
                foreach (var process in selectedProcesses)
                {
                    // 1. Crear un nuevo resultado para el historial global
                    var scanResult = new ScanResultItem { ProcessName = process.ProcessName, ScanResult = "Scanning..." };
                    ScanResultsManager.AddScanResult(scanResult); // Añadir al manager compartido

                    // 2. Mantener la lógica original para feedback inmediato en la ProcessesPage
                    process.VirusStatus = "Scanning...";
                    try
                    {
                        var runningProcess = _processManagementService.GetProcessById(process.ProcessId);
                        if (runningProcess != null)
                        {
                            var path = _processManagementService.GetProcessPath(runningProcess);
                            if (!string.IsNullOrEmpty(path) && File.Exists(path))
                            {
                                var result = await _virusScanService.GetVirusTotalReportAsync(path);
                                process.VirusStatus = result; // Actualizar estado en la ProcessesPage
                                scanResult.ScanResult = result; // Actualizar estado en el historial global
                            }
                            else
                            {
                                var noPathResult = "No file path";
                                process.VirusStatus = noPathResult;
                                scanResult.ScanResult = noPathResult;
                            }
                        }
                        else
                        {
                            var notFoundResult = "Process not found";
                            process.VirusStatus = notFoundResult;
                            scanResult.ScanResult = notFoundResult;
                        }
                    }
                    catch (Exception ex)
                    {
                        var errorResult = $"Error: {ex.Message}";
                        process.VirusStatus = errorResult;
                        scanResult.ScanResult = errorResult;
                    }
                }

                _notificationService.ShowSuccess($"Scanned {selectedProcesses.Count} process(es).");
            }
        }   

        /// <summary>
        /// QuerySubmitted event handler for the SearchBox
        /// Filters processes based on search text
        /// </summary>
        private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (string.IsNullOrWhiteSpace(args.QueryText))
            {
                LoadProcesses();
                return;
            }

            var filtered = _processSearchService.SearchProcesses(Processes, args.QueryText).ToList();
            Processes.Clear();
            foreach (var process in filtered)
            {
                Processes.Add(process);
            }
        }

        /// <summary>
        /// TextChanged event handler for the SearchBox
        /// Updates search suggestions
        /// </summary>
        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                if (string.IsNullOrWhiteSpace(sender.Text))
                {
                    LoadProcesses();
                    return;
                }

                var suggestions = _processSearchService.GetSearchSuggestions(Processes, sender.Text);
                sender.ItemsSource = suggestions;
            }
        }

        /// <summary>
        /// SuggestionChosen event handler for the SearchBox
        /// Sets the selected text in the SearchBox
        /// </summary>
        private void SearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is string processName)
            {
                sender.Text = processName;
            }
        }

        /// <summary>
        /// Click event handler to sort by name
        /// </summary>
        private void OrderBy_Name(object sender, RoutedEventArgs e)
        {
            var isAscending = _uiSortingService.ToggleSortState("name", SortByNameIcon_Name);
            var sorted = _processSearchService.SortProcesses(allProcesses, "name", isAscending).ToList();
            allProcesses = sorted;
            RefreshProcessesFromAll();
        }

        /// <summary>
        /// Click event handler to sort by ID
        /// </summary>
        private void OrderBy_Id(object sender, RoutedEventArgs e)
        {
            var isAscending = _uiSortingService.ToggleSortState("id", SortByNameIcon_Id);
            var sorted = _processSearchService.SortProcesses(allProcesses, "id", isAscending).ToList();
            allProcesses = sorted;
            RefreshProcessesFromAll();
        }

        /// <summary>
        /// Checked event handler for the process CheckBox
        /// </summary>
        private void ProcessCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            UpdateSelectionState();
        }

        /// <summary>
        /// Unchecked event handler for the process CheckBox
        /// </summary>
        private void ProcessCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateSelectionState();
        }

        /// <summary>
        /// ItemClick event handler for the process ListView
        /// </summary>
        private void ProcessListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is ProcessItem process)
            {
                _notificationService.ShowSuccess($"Process: {process.ProcessName} (ID: {process.ProcessId})");
            }
        }

        /// <summary>
        /// Click event handler to view process details
        /// </summary>
        private async void ViewDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ProcessItem process)
            {
                try
                {
                    var currentWindow = App.MainWindow;
                    if (currentWindow == null)
                    {
                        _notificationService.ShowError("Cannot show dialog - Window is null");
                        return;
                    }

                    await _processDetailsDialogService.ShowProcessDetailsAsync(process, currentWindow.Content.XamlRoot);
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Error: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"ViewDetails error: {ex}");
                }
            }
        }
    }
}