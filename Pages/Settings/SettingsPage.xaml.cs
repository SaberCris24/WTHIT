using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using WTHIT.Core;
using WTHIT.Services;
using System;

namespace WTHIT.Pages.Settings
{
    /// <summary>
    /// Settings page for configuring application preferences.
    /// Manages theme, backdrop, navigation, API keys, and cache settings.
    /// </summary>
    public sealed partial class SettingsPage : Page, IDisposable
    {
        private bool _disposed;
        private bool _initializing = true;

        /// <summary>
        /// Page constructor. Initializes UI components and event handlers.
        /// </summary>
        public SettingsPage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Disabled;
            Loaded += OnPageLoaded;
        }

        /// <summary>
        /// Event handler triggered when the page is loaded into the visual tree.
        /// Loads the saved settings into the page controls.
        /// </summary>
        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            
            InitializeSettings();
            UpdateCacheSizeDisplay();
            _initializing = false;
        }

        /// <summary>
        /// Initializes UI controls with current settings values.
        /// </summary>
        private void InitializeSettings()
        {
            txtVirusTotalApiKey.Password = Configuration.VirusTotalApiKey;
            cmbTheme.SelectedIndex = App.CurrentTheme switch
            {
                ElementTheme.Light => 1,
                ElementTheme.Dark => 2,
                _ => 0
            };
            cmbBackdrop.SelectedIndex = App.CurrentBackdrop switch
            {
                BackdropType.MicaAlt => 1,
                BackdropType.Acrylic => 2,
                _ => 0
            };
            if (App.MainWindow is MainWindow mainWindow)
            {
                cmbNavPosition.SelectedIndex = mainWindow.NavigationViewControl.PaneDisplayMode == NavigationViewPaneDisplayMode.Top ? 1 : 0;
            }
            numMaxScanResults.Minimum = Configuration.MinScanResults;
            numMaxScanResults.Maximum = Configuration.MaxScanResultsLimit;
            numMaxScanResults.Value = Configuration.MaxScanResults;
        }

        /// <summary>
        /// Updates the cache size display with current statistics.
        /// </summary>
        private void UpdateCacheSizeDisplay()
        {
            try
            {
                var appInfoService = ApplicationInfoService.Shared;
                var stats = appInfoService.GetCacheStats();
                var total = stats.ProcessCache + stats.RegistryCache + stats.StoreAppCache;
                CacheSizeText.Text = $"Current cache: {total} items (Process: {stats.ProcessCache}, Registry: {stats.RegistryCache}, Store: {stats.StoreAppCache})";
            }
            catch
            {
                CacheSizeText.Text = "Current cache: unavailable";
            }
        }

        /// <summary>
        /// SelectionChanged event handler for the theme ComboBox.
        /// </summary>
        private void cmbTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_initializing || sender is not ComboBox { SelectedItem: ComboBoxItem selectedItem }) return;
            var theme = selectedItem.Tag.ToString() switch
            {
                "Light" => ElementTheme.Light,
                "Dark" => ElementTheme.Dark,
                _ => ElementTheme.Default
            };
            App.SetTheme(theme);
        }

        /// <summary>
        /// SelectionChanged event handler for the backdrop ComboBox.
        /// </summary>
        private void cmbBackdrop_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_initializing || sender is not ComboBox { SelectedItem: ComboBoxItem selectedItem }) return;
            var backdrop = selectedItem.Tag.ToString() switch
            {
                "MicaAlt" => BackdropType.MicaAlt,
                "Acrylic" => BackdropType.Acrylic,
                _ => BackdropType.Mica
            };
            App.SetBackdrop(backdrop);
        }

        /// <summary>
        /// SelectionChanged event handler for the navigation position ComboBox.
        /// </summary>
        private void cmbNavPosition_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_initializing || sender is not ComboBox { SelectedItem: ComboBoxItem selectedItem }) return;
            if (App.MainWindow is not MainWindow mainWindow) return;
            var isLeftMode = (string)selectedItem.Tag == "Left";
            mainWindow.NavigationViewControl.PaneDisplayMode = isLeftMode
                ? NavigationViewPaneDisplayMode.Auto
                : NavigationViewPaneDisplayMode.Top;
            mainWindow.NavigationViewControl.IsPaneOpen = isLeftMode;
            mainWindow.SaveNavigationViewPosition(isLeftMode);
        }

        /// <summary>
        /// Click event handler for the Save API Key button.
        /// </summary>
        private void btnSaveApiKey_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var apiKey = txtVirusTotalApiKey.Password.Trim();
                Configuration.VirusTotalApiKey = apiKey;
                txtVirusTotalApiKey.Password = apiKey;

                if (apiKey.Length == 0)
                {
                    NotificationService.Instance.ShowWarning("API key cleared. VirusTotal scanning will be disabled.");
                }
                else
                {
                    NotificationService.Instance.ShowSuccess("VirusTotal API key saved successfully.");
                }
            }
            catch (Exception ex)
            {
                NotificationService.Instance.ShowError($"Error saving API key: {ex.Message}");
            }
        }

        /// <summary>
        /// Click event handler for the Clear Cache button.
        /// </summary>
        private void btnClearCache_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ApplicationInfoService.Shared.ClearCache();
                UpdateCacheSizeDisplay();
                NotificationService.Instance.ShowSuccess("Cache cleared successfully.");
            }
            catch (Exception ex)
            {
                NotificationService.Instance.ShowError($"Error clearing cache: {ex.Message}");
            }
        }

        /// <summary>
        /// Click event handler for the Clear Scan History button.
        /// </summary>
        private async void btnClearScanHistory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await ScanHistoryService.Shared.ClearAsync();
                NotificationService.Instance.ShowSuccess("Scan history cleared successfully.");
            }
            catch (Exception ex)
            {
                NotificationService.Instance.ShowError($"Failed to clear scan history: {ex.Message}");
            }
        }

        /// <summary>
        /// Click event handler for the Reset Settings button.
        /// </summary>
        private void btnResetSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                App.SetTheme(ElementTheme.Default);
                cmbTheme.SelectedIndex = 0;
                App.SetBackdrop(BackdropType.Mica);
                cmbBackdrop.SelectedIndex = 0;
                if (App.MainWindow is MainWindow mainWindow)
                {
                    mainWindow.NavigationViewControl.PaneDisplayMode = NavigationViewPaneDisplayMode.Auto;
                    mainWindow.NavigationViewControl.IsPaneOpen = true;
                    mainWindow.SaveNavigationViewPosition(true);
                    cmbNavPosition.SelectedIndex = 0;
                }
                Configuration.MaxScanResults = Configuration.DefaultMaxScanCount;
                numMaxScanResults.Value = Configuration.DefaultMaxScanCount;
                Configuration.VirusTotalApiKey = string.Empty;
                txtVirusTotalApiKey.Password = string.Empty;
                ApplicationInfoService.Shared.ClearCache();
                UpdateCacheSizeDisplay();
                NotificationService.Instance.ShowSuccess("All settings reset to defaults.");
            }
            catch (Exception ex)
            {
                NotificationService.Instance.ShowError($"Error resetting settings: {ex.Message}");
            }
        }

        /// <summary>
        /// ValueChanged event handler for the Max Scan Results NumberBox.
        /// </summary>
        private void numMaxScanResults_ValueChanged(object sender, NumberBoxValueChangedEventArgs e)
        {
            if (_initializing) return;
            Configuration.MaxScanResults = (int)numMaxScanResults.Value;
        }

        /// <summary>
        /// Releases all page resources and detaches event handlers.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            Loaded -= OnPageLoaded;
            _disposed = true;
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
}
