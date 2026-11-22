using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
namespace WTHIT.Core
{
    /// <summary>
    /// Manages navigation view position and state
    /// </summary>
    public sealed class NavigationManager
    {
        private static readonly Lazy<NavigationManager> _instance = new(() => new NavigationManager());
        private const string NavPositionKey = "NavIsLeftMode";
        private const string NavPaneOpenKey = "NavIsPaneOpen";
        private bool _isLeftMode = true;
        private bool _isPaneOpen = false;
        private bool _initialized;
        private bool _suppressPaneOpenSave;

        private NavigationManager() { }

        public static NavigationManager Instance => _instance.Value;
        public bool IsLeftMode => _isLeftMode;
        public event EventHandler<bool>? PositionChanged;

        public void Initialize(NavigationView navView)
        {
            if (navView == null) return;
            if (_initialized)
            {
                ApplyPosition(navView, IsLeftMode);
                return;
            }

            try
            {
                _isLeftMode = LoadPosition();
                _isPaneOpen = LoadPaneOpenState();
                _initialized = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize navigation: {ex.Message}");
                _isLeftMode = true;
                _isPaneOpen = false;
                _initialized = true;
            }

            ApplyPosition(navView, _isLeftMode);
            navView.RegisterPropertyChangedCallback(NavigationView.IsPaneOpenProperty, OnIsPaneOpenChanged);

            // Initialization may run after the Loaded event.
            if (navView.IsLoaded)
            {
                navView.LayoutUpdated += NavView_LayoutUpdated;
            }
            else
            {
                navView.Loaded += NavView_Loaded;
            }
        }

        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not NavigationView navView) return;
            navView.Loaded -= NavView_Loaded;

            // Wait for the NavigationView to determine its display mode during layout.
            navView.LayoutUpdated += NavView_LayoutUpdated;
        }

        private void NavView_LayoutUpdated(object? sender, object e)
        {
            if (sender is not NavigationView navView) return;

            navView.LayoutUpdated -= NavView_LayoutUpdated;

            _suppressPaneOpenSave = true;
            try
            {
                navView.IsPaneOpen = _isLeftMode && _isPaneOpen;
            }
            finally
            {
                _suppressPaneOpenSave = false;
            }
        }

        private void OnIsPaneOpenChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (_suppressPaneOpenSave) return;
            if (sender is not NavigationView navView) return;
            
            if (navView.PaneDisplayMode == NavigationViewPaneDisplayMode.Top) return;

            _isPaneOpen = navView.IsPaneOpen;
            SavePaneOpenState(_isPaneOpen);
        }

        public void SetPosition(NavigationView navView, bool isLeftMode)
        {
            if (navView == null || IsLeftMode == isLeftMode) return;

            _isLeftMode = isLeftMode;
            SavePosition(isLeftMode);
            ApplyPosition(navView, isLeftMode);
            PositionChanged?.Invoke(this, isLeftMode);
        }

        public void UpdatePosition(NavigationView navView, bool isLeftMode) =>
            SetPosition(navView, isLeftMode);

        private void ApplyPosition(NavigationView navView, bool isLeftMode)
        {
            if (navView == null) return;

            _suppressPaneOpenSave = true;
            try
            {
                navView.PaneDisplayMode = isLeftMode
                    ? NavigationViewPaneDisplayMode.Left
                    : NavigationViewPaneDisplayMode.Top;
                
                navView.IsPaneOpen = isLeftMode && _isPaneOpen;
            }
            finally
            {
                _suppressPaneOpenSave = false;
            }
        }

        private bool LoadPosition()
        {
            try
            {
                return SettingsManager.Instance.GetValue(NavPositionKey, true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load navigation position: {ex.Message}");
                return true;
            }
        }

        private void SavePosition(bool isLeftMode)
        {
            try
            {
                SettingsManager.Instance.SetValue(NavPositionKey, isLeftMode);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save navigation position: {ex.Message}");
            }
        }

        private bool LoadPaneOpenState()
        {
            try
            {
                return SettingsManager.Instance.GetValue(NavPaneOpenKey, false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load pane open state: {ex.Message}");
                return false;
            }
        }

        private void SavePaneOpenState(bool isPaneOpen)
        {
            try
            {
                SettingsManager.Instance.SetValue(NavPaneOpenKey, isPaneOpen);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save pane open state: {ex.Message}");
            }
        }
    }
}