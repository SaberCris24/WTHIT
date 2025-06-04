using Microsoft.UI.Xaml;
using Windows.Storage;
using System;
using Plantilla.Helpers;

namespace Plantilla
{
    /// <summary>
    /// Main application class that handles window management, themes, and backdrops
    /// </summary>
    public partial class App : Application
    {
        // Main application window reference
        public static Window MainWindow { get; private set; } = null!;
        
        // Tracks the current application theme
        public static ElementTheme CurrentTheme { get; private set; } = ElementTheme.Default;
        
        // Tracks the current backdrop effect
        public static BackdropHelper.BackdropType CurrentBackdrop { get; private set; } = BackdropHelper.BackdropType.Mica;

        /// <summary>
        /// Initializes the application components
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Called when the application is launched
        /// Sets up the main window, backdrop, and theme
        /// </summary>
        /// <param name="args">Launch activation arguments</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            try
            {
                // Create and configure the main window
                MainWindow = new MainWindow(800, 400);
                MainWindow.ExtendsContentIntoTitleBar = true;

                // Set initial backdrop
                CurrentBackdrop = BackdropHelper.BackdropType.Mica;
                BackdropHelper.SetBackdrop(MainWindow, CurrentBackdrop);

                // Load and apply saved theme
                if (MainWindow.Content is FrameworkElement rootElement)
                {
                    ElementTheme savedTheme = LoadSavedTheme();
                    rootElement.RequestedTheme = savedTheme;
                    CurrentTheme = savedTheme;
                }

                MainWindow.Activate();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnLaunched: {ex.Message}");
                // Set defaults if initialization fails
                CurrentTheme = ElementTheme.Default;
            }
        }

        /// <summary>
        /// Sets the application theme and persists the selection
        /// </summary>
        /// <param name="theme">The theme to apply</param>
        public static void SetTheme(ElementTheme theme)
        {
            try
            {
                if (MainWindow?.Content is FrameworkElement rootElement)
                {
                    rootElement.RequestedTheme = theme;
                    CurrentTheme = theme;
                    SaveTheme(theme);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting theme: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets the window backdrop effect
        /// </summary>
        /// <param name="type">The backdrop type to apply</param>
        public static void SetBackdrop(BackdropHelper.BackdropType type)
        {
            CurrentBackdrop = type;
            BackdropHelper.SetBackdrop(MainWindow, type);
        }

        /// <summary>
        /// Persists the theme selection to application settings
        /// </summary>
        /// <param name="theme">Theme to save</param>
        private static void SaveTheme(ElementTheme theme)
        {
            try
            {
                ApplicationData.Current.LocalSettings.Values["AppTheme"] = theme.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving theme: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads the previously saved theme from application settings
        /// </summary>
        /// <returns>The saved theme or Default if none found</returns>
        private static ElementTheme LoadSavedTheme()
        {
            try
            {
                var savedTheme = ApplicationData.Current?.LocalSettings?.Values["AppTheme"] as string;
                return savedTheme switch
                {
                    nameof(ElementTheme.Dark) => ElementTheme.Dark,
                    nameof(ElementTheme.Light) => ElementTheme.Light,
                    _ => ElementTheme.Default
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading theme: {ex.Message}");
                return ElementTheme.Default;
            }
        }
    }
}