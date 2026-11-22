using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using WTHIT.Core;
using WTHIT.Helpers;
using WTHIT.Pages;
using WTHIT.Pages.Home;
using WTHIT.Pages.Processes;
using WTHIT.Pages.About;
using WTHIT.Pages.Settings;
using WTHIT.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WTHIT
{
    public sealed partial class MainWindow : Window, IDisposable
    {
        private bool _disposed;
        private bool _eventsRegistered;
        private readonly AppWindowTitleBar? _titleBar;

        private static readonly Dictionary<string, Type> _pages = new()
        {
            ["home"] = typeof(HomePage),
            ["processes"] = typeof(ProcessesPage),
            ["about"] = typeof(AboutPage)
        };

        private static readonly Type DefaultPage = typeof(HomePage);

        public NavigationView NavigationViewControl => NavView;

        /// <summary>
        /// Shows a dialog inside the window's full root grid. Popup placement can inherit a
        /// reduced layout area when a custom title bar and NavigationView are present, which
        /// leaves dialogs visually aligned to the left instead of to the whole window.
        /// </summary>
        public async Task<ContentDialogResult> ShowDialogAsync(ContentDialog dialog)
        {
            ArgumentNullException.ThrowIfNull(dialog);

            DialogHost.Children.Add(dialog);
            try
            {
                return await dialog.ShowAsync(ContentDialogPlacement.InPlace);
            }
            finally
            {
                DialogHost.Children.Remove(dialog);
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            WindowSetupHelper.SetupWindow(this, 1000, 500, 1000, 500);
            WindowSetupHelper.SetupWindowIcon(this);

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            try
            {
                _titleBar = AppWindow?.TitleBar;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error obteniendo TitleBar: {ex.Message}");
            }

            NavigationManager.Instance.Initialize(NavView);
            NotificationService.Instance.Initialize(NotificationHost);

            NavigationManager.Instance.PositionChanged += NavigationManager_PositionChanged;

            SetupEventHandlers();

            ApplyTitleBarLayout(NavigationManager.Instance.IsLeftMode);

            contentFrame.Navigate(DefaultPage);
        }

        private void SetupEventHandlers()
        {
            if (_eventsRegistered) return;
            _eventsRegistered = true;

            NavView.BackRequested += NavView_BackRequested;
            NavView.SelectionChanged += NavView_SelectionChanged;
            contentFrame.Navigated += contentFrame_Navigated;

            if (Content is FrameworkElement rootElement)
            {
                rootElement.ActualThemeChanged += RootElement_ActualThemeChanged;
                UpdateCaptionButtons(rootElement.ActualTheme);
            }

            this.Activated += MainWindow_Activated;
            this.Closed += MainWindow_Closed;
        }

        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            TitleBarHelper.UpdateForWindowActivation(this, args.WindowActivationState != WindowActivationState.Deactivated);
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            WindowSetupHelper.SaveWindowPosition(this);
            (contentFrame.Content as IDisposable)?.Dispose();

            if (App.Current is App app)
            {
                app.Dispose();
            }

            Dispose();
        }

        public void SaveNavigationViewPosition(bool isLeftMode) =>
            NavigationManager.Instance.SetPosition(NavView, isLeftMode);

        public void UpdateNavigationViewMode(bool isLeftMode) =>
            NavigationManager.Instance.UpdatePosition(NavView, isLeftMode);

        private void NavigationManager_PositionChanged(object? sender, bool isLeftMode) =>
            ApplyTitleBarLayout(isLeftMode);

        /// <summary>
        /// Places top navigation below the title bar and lets side navigation span both rows.
        /// </summary>
        private void ApplyTitleBarLayout(bool isLeftMode)
        {
            if (isLeftMode)
            {
                AppTitleBar.Margin = new Thickness(48, 0, 0, 0);
                Grid.SetRow(AppTitleBar, 0);
                Grid.SetRowSpan(AppTitleBar, 1);
                Grid.SetRow(NavView, 0);
                Grid.SetRowSpan(NavView, 2);
            }
            else
            {
                AppTitleBar.Margin = new Thickness(16, 0, 0, 0);
                Grid.SetRow(AppTitleBar, 0);
                Grid.SetRowSpan(AppTitleBar, 1);
                Grid.SetRow(NavView, 1);
                Grid.SetRowSpan(NavView, 1);
            }
        }

        private void RootElement_ActualThemeChanged(FrameworkElement sender, object args) =>
            UpdateCaptionButtons(sender.ActualTheme);

        private void UpdateCaptionButtons(ElementTheme theme)
        {
            if (_titleBar is null) return;

            try
            {
                var effectiveTheme = ThemeManager.GetEffectiveTheme(theme);
                var color = effectiveTheme == ElementTheme.Dark ? Colors.White : Colors.Black;

                _titleBar.ButtonForegroundColor = color;
                _titleBar.ButtonHoverForegroundColor = color;
                _titleBar.ButtonPressedForegroundColor = color;
                _titleBar.ButtonInactiveForegroundColor = Colors.Gray;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to update the title bar buttons: {ex.Message}");
            }
        }

        private void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            if (contentFrame.CanGoBack)
            {
                contentFrame.GoBack();
            }
        }

        private void contentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            NavView.SelectionChanged -= NavView_SelectionChanged;

            try
            {
                NavView.IsBackEnabled = contentFrame.CanGoBack;
                NavView.Header = null;

                NavView.SelectedItem = contentFrame.CurrentSourcePageType switch
                {
                    Type t when t == typeof(HomePage) => HomeItem,
                    Type t when t == typeof(ProcessesPage) => ProcessesItem,
                    Type t when t == typeof(AboutPage) => AboutItem,
                    Type t when t == typeof(SettingsPage) => NavView.SettingsItem,
                    _ => null
                };
            }
            finally
            {
                NavView.SelectionChanged += NavView_SelectionChanged;
            }
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            Type? targetType = null;

            if (args.IsSettingsSelected)
            {
                targetType = typeof(SettingsPage);
            }
            else if (args.SelectedItem is NavigationViewItem selectedItem && selectedItem.Tag is string tag)
            {
                _pages.TryGetValue(tag, out targetType);
            }

            if (targetType != null && contentFrame.CurrentSourcePageType != targetType)
            {
                contentFrame.Navigate(targetType);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            NotificationService.Instance.Detach(NotificationHost);
            NavigationManager.Instance.PositionChanged -= NavigationManager_PositionChanged;

            NavView.BackRequested -= NavView_BackRequested;
            NavView.SelectionChanged -= NavView_SelectionChanged;
            contentFrame.Navigated -= contentFrame_Navigated;

            if (Content is FrameworkElement rootElement)
            {
                rootElement.ActualThemeChanged -= RootElement_ActualThemeChanged;
            }

            this.Activated -= MainWindow_Activated;
            this.Closed -= MainWindow_Closed;

            GC.SuppressFinalize(this);
        }
    }
}
