using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Windowing;
using WTHIT.Core;
using WTHIT.Services;
using System;

namespace WTHIT
{
    /// <summary>
    /// Main application class
    /// </summary>
    public partial class App : Application, IDisposable
    {
        private static MainWindow? _mainWindow;
        private bool _disposed;

        public static MainWindow MainWindow => _mainWindow ?? throw new InvalidOperationException("MainWindow not initialized yet.");

        public static ElementTheme CurrentTheme => ThemeManager.Instance.CurrentTheme;
        public static BackdropType CurrentBackdrop => BackdropManager.Instance.CurrentBackdrop;

        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            if (_mainWindow is null)
            {
                _mainWindow = new MainWindow();
            }

            ThemeManager.Instance.Initialize();
            BackdropManager.Instance.Initialize();

            if (_mainWindow.Content is FrameworkElement rootElement)
            {
                var effectiveTheme = ThemeManager.GetEffectiveTheme(CurrentTheme);
                rootElement.RequestedTheme = effectiveTheme;
            }

            BackdropManager.Instance.SetBackdrop(_mainWindow, BackdropManager.Instance.CurrentBackdrop);
            ThemeManager.Instance.ApplyTheme(ThemeManager.Instance.CurrentTheme);

            if (_mainWindow.AppWindow.Presenter is OverlappedPresenter { State: OverlappedPresenterState.Minimized } presenter)
                presenter.Restore();

            _mainWindow.Activate();
        }

        public static void SetTheme(ElementTheme theme) => ThemeManager.Instance.SetTheme(theme);

        public static void SetBackdrop(BackdropType type)
        {
            if (_mainWindow != null)
                BackdropManager.Instance.SetBackdrop(_mainWindow, type);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                DatabaseService.Instance.DisposeSync();
                VirusScanService.Shared.Dispose();
            }
            finally
            {
                _mainWindow = null;
            }

            GC.SuppressFinalize(this);
        }
    }
}
