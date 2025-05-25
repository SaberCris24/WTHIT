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
using Plantilla.Services;

namespace Plantilla.Pages.Processes
{
    /// <summary>
    /// Página principal que muestra y gestiona los procesos del sistema
    /// </summary>
    public sealed partial class ProcessesPage : Page, INotifyPropertyChanged
    {
        // Colección observable de procesos que se muestra en la UI
        private ObservableCollection<ProcessItem> Processes { get; set; }
        
        // Variables de estado
        private bool _isProcessSelected;
        private List<ProcessItem> allProcesses;
        private static bool _processesLoaded = false;
        
        // Servicios inyectados
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
        /// Propiedad que indica si hay algún proceso seleccionado
        /// Notifica a la UI cuando cambia su valor
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
        /// Constructor de la página
        /// Inicializa los componentes y servicios necesarios
        /// </summary>
        public ProcessesPage()
        {
            // Inicialización de componentes UI
            this.InitializeComponent();
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
            Processes = new ObservableCollection<ProcessItem>();
            ProcessListView.ItemsSource = Processes;

            // Inicialización de servicios
            _databaseService = new DatabaseService();
            _virusScanService = new VirusScanService("1234"); // Reemplaza con tu API key real
            _applicationInfoService = new ApplicationInfoService();
            _processSearchService = new ProcessSearchService();
            _processDetailsDialogService = new ProcessDetailsDialogService(_virusScanService, _databaseService);
            _notificationService = new NotificationService();
            _processManagementService = new ProcessManagementService(_applicationInfoService);
            _processSelectionService = new ProcessSelectionService();
            _uiSortingService = new UISortingService();
            
            // Inicializar el servicio de notificaciones
            _notificationService.Initialize(StatusInfoBar);

            // Inicializar la base de datos
            _ = _databaseService.InitializeAsync();

            // Cargar procesos iniciales
            if (allProcesses == null)
            {
                allProcesses = new List<ProcessItem>();
                LoadProcessesToAll();
            }
            else
            {
                RefreshProcessesFromAll();
            }

            // Configurar evento Loaded
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
        /// Carga todos los procesos en la lista allProcesses
        /// </summary>
        private void LoadProcessesToAll()
        {
            allProcesses.Clear();
            allProcesses.AddRange(_processManagementService.GetAllProcesses());
            RefreshProcessesFromAll();
        }

        /// <summary>
        /// Actualiza la colección observable desde allProcesses
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
        /// Carga los procesos del sistema en la colección observable
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
        /// Actualiza el estado de selección de procesos
        /// </summary>
        private void UpdateSelectionState()
        {
            IsProcessSelected = _processSelectionService.HasSelectedProcesses(Processes);
        }

        /// <summary>
        /// Manejador del evento Click del botón Refresh
        /// </summary>
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadProcesses();
        }

        /// <summary>
        /// Manejador del evento Click del botón Scan
        /// Escanea los procesos seleccionados en busca de virus
        /// </summary>
        private async void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedProcesses = _processSelectionService.GetSelectedProcesses(Processes);
            if (selectedProcesses.Any())
            {
                foreach (var process in selectedProcesses)
                {
                    process.VirusStatus = "Scanning...";
                    try
                    {
                        var runningProcess = _processManagementService.GetProcessById(process.ProcessId);
                        if (runningProcess != null)
                        {
                            var path = _processManagementService.GetProcessPath(runningProcess);
                            if (!string.IsNullOrEmpty(path) && File.Exists(path))
                            {
                                process.VirusStatus = await _virusScanService.GetVirusTotalReportAsync(path);
                            }
                            else
                            {
                                process.VirusStatus = "No file path";
                            }
                        }
                        else
                        {
                            process.VirusStatus = "Process not found";
                        }
                    }
                    catch (Exception ex)
                    {
                        process.VirusStatus = $"Error: {ex.Message}";
                    }
                }

                _notificationService.ShowSuccess($"Scanned {selectedProcesses.Count} process(es).");
            }
        }

        /// <summary>
        /// Manejador del evento QuerySubmitted del SearchBox
        /// Filtra los procesos según el texto de búsqueda
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
        /// Manejador del evento TextChanged del SearchBox
        /// Actualiza las sugerencias de búsqueda
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
        /// Manejador del evento SuggestionChosen del SearchBox
        /// Establece el texto seleccionado en el SearchBox
        /// </summary>
        private void SearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is string processName)
            {
                sender.Text = processName;
            }
        }

        /// <summary>
        /// Manejador del evento Click para ordenar por nombre
        /// </summary>
        private void OrderBy_Name(object sender, RoutedEventArgs e)
        {
            var isAscending = _uiSortingService.ToggleSortState("name", SortByNameIcon_Name);
            var sorted = _processSearchService.SortProcesses(allProcesses, "name", isAscending).ToList();
            allProcesses = sorted;
            RefreshProcessesFromAll();
        }

        /// <summary>
        /// Manejador del evento Click para ordenar por ID
        /// </summary>
        private void OrderBy_Id(object sender, RoutedEventArgs e)
        {
            var isAscending = _uiSortingService.ToggleSortState("id", SortByNameIcon_Id);
            var sorted = _processSearchService.SortProcesses(allProcesses, "id", isAscending).ToList();
            allProcesses = sorted;
            RefreshProcessesFromAll();
        }

        /// <summary>
        /// Manejador del evento Checked del CheckBox de proceso
        /// </summary>
        private void ProcessCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            UpdateSelectionState();
        }

        /// <summary>
        /// Manejador del evento Unchecked del CheckBox de proceso
        /// </summary>
        private void ProcessCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateSelectionState();
        }

        /// <summary>
        /// Manejador del evento ItemClick del ListView de procesos
        /// </summary>
        private void ProcessListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is ProcessItem process)
            {
                _notificationService.ShowSuccess($"Process: {process.ProcessName} (ID: {process.ProcessId})");
            }
        }

        /// <summary>
        /// Manejador del evento Click para ver detalles de un proceso
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