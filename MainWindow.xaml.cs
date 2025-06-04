using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Plantilla.Helpers;
using Plantilla.Pages.About;
using Plantilla.Pages.Processes;
using Plantilla.Pages.Settings;
using System;
using System.Runtime.InteropServices;
using Windows.Storage;
using WinRT.Interop;

namespace Plantilla
{
    /// <summary>
    /// Main window class that handles the application's primary window functionality
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        #region Win32 API

        // Import Windows API function to send messages to windows
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        // Import Windows API function to load images (icons in this case)
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr LoadImage(IntPtr hInst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

        // Constants for Windows API calls
        private const int WM_SETICON = 0x0080;      // Message to set window icon
        private const uint IMAGE_ICON = 1;          // Specifies that the image is an icon
        private const uint LR_LOADFROMFILE = 0x00000010;  // Load image from file flag

        #endregion

        #region Properties

        /// <summary>
        /// Public property to access the NavigationView control from other parts of the app
        /// </summary>
        public NavigationView NavigationViewControl => NavView;

        /// <summary>
        /// Stores the current theme color for the caption buttons
        /// </summary>
        private Windows.UI.Color _currentCaptionButtonsColor;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor for the main window
        /// </summary>
        /// <param name="MinWidth">Minimum width of the window</param>
        /// <param name="MinHeight">Minimum height of the window</param>
        public MainWindow(int MinWidth, int MinHeight)
        {
            try
            {
                // Initialize the window components
                this.InitializeComponent();

                // Configure the title bar and window
                SetupTitleBar();
                SetupWindowSize(MinWidth, MinHeight);
                SetupWindowIcon();

                // Setup initial theme and colors
                SetupTheme();

                // Navigate to the default page (Processes)
                contentFrame.Navigate(typeof(ProcessesPage));

                // Initialize NavigationView position from settings
                LoadNavigationViewPosition();

                // Subscribe to events
                NavView.DisplayModeChanged += NavView_DisplayModeChanged;
                if (Content is FrameworkElement rootElement)
                {
                    rootElement.ActualThemeChanged += MainWindow_ActualThemeChanged;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing MainWindow: {ex.Message}");
            }
        }

        #endregion

        #region Window Setup Methods

        /// <summary>
        /// Configures the title bar appearance and behavior
        /// </summary>
        private void SetupTitleBar()
        {
            try
            {
                // Enable custom title bar
                ExtendsContentIntoTitleBar = true;
                SetTitleBar(AppTitleBar);

                // Set window title
                AppWindow.Title = "WTHIT";

                // Configure the native title bar
                AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
                AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;

                // Set initial colors
                TitleBarHelper.SetBackgroundColor(this, Colors.Transparent);
                _currentCaptionButtonsColor = TitleBarHelper.ApplySystemThemeToCaptionButtons(this);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting up title bar: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets up the window icon
        /// </summary>
        private void SetupWindowIcon()
        {
            try
            {
                var hwnd = WindowNative.GetWindowHandle(this);
                string iconPath = System.IO.Path.Combine(Environment.CurrentDirectory, "Assets", "icon.ico");
                
                // Load the icon from file
                IntPtr hIcon = LoadImage(IntPtr.Zero, iconPath, IMAGE_ICON, 0, 0, LR_LOADFROMFILE);
                
                // Set both large and small icons for the window
                SendMessage(hwnd, WM_SETICON, (IntPtr)1, hIcon); // ICON_BIG
                SendMessage(hwnd, WM_SETICON, (IntPtr)0, hIcon); // ICON_SMALL
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting window icon: {ex.Message}");
            }
        }

        /// <summary>
        /// Configures the window size constraints and initial state
        /// </summary>
    private void SetupWindowSize(int MinWidth, int MinHeight)
    {
        try
        {
            // Create and configure the window presenter
            var presenter = OverlappedPresenter.Create();
            presenter.IsResizable = true;
            presenter.IsMaximizable = true;
            presenter.IsMinimizable = true;

            // Set minimum window size using the presenter's size constraints
            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            
            // Set the minimum size
            appWindow.Resize(new Windows.Graphics.SizeInt32(MinWidth, MinHeight));

            // Apply the presenter to the window
            AppWindow.SetPresenter(presenter);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error setting up window size: {ex.Message}");
        }
    }

        /// <summary>
        /// Sets up the initial theme and related visual elements
        /// </summary>
        private void SetupTheme()
        {
            try
            {
                if (Content is FrameworkElement rootElement)
                {
                    // Load the saved theme or use system default
                    ElementTheme savedTheme = LoadSavedTheme();
                    rootElement.RequestedTheme = savedTheme;

                    // Update caption buttons to match the theme
                    UpdateCaptionButtonsForTheme(rootElement.ActualTheme);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting up theme: {ex.Message}");
            }
        }

        #endregion

        #region Theme Management

        /// <summary>
        /// Handles theme changes and updates the UI accordingly
        /// </summary>
        private void MainWindow_ActualThemeChanged(FrameworkElement sender, object args)
        {
            try
            {
                UpdateCaptionButtonsForTheme(sender.ActualTheme);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling theme change: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates caption button colors based on the current theme
        /// </summary>
        private void UpdateCaptionButtonsForTheme(ElementTheme theme)
        {
            _currentCaptionButtonsColor = theme == ElementTheme.Dark ? Colors.White : Colors.Black;
            TitleBarHelper.SetCaptionButtonColors(this, _currentCaptionButtonsColor);
        }

        /// <summary>
        /// Loads the saved theme from application settings
        /// </summary>
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

        #endregion

        #region Navigation

        /// <summary>
        /// Event handler for navigation view selection changes
        /// </summary>
        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            try
            {
                if (args.IsSettingsSelected)
                {
                    contentFrame.Navigate(typeof(SettingsPage));
                }
                else if (args.SelectedItem is NavigationViewItem selectedItem)
                {
                    switch (selectedItem.Tag.ToString())
                    {
                        case "processes":
                            contentFrame.Navigate(typeof(ProcessesPage));
                            break;
                        case "about":
                            contentFrame.Navigate(typeof(AboutPage));
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling navigation: {ex.Message}");
            }
        }

        /// <summary>
        /// Event handler for NavigationView DisplayMode changes
        /// </summary>
        private void NavView_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
        {
            SaveNavigationViewPosition(sender.PaneDisplayMode != NavigationViewPaneDisplayMode.Top);
        }

        /// <summary>
        /// Loads the saved NavigationView position from settings
        /// </summary>
        private void LoadNavigationViewPosition()
        {
            try
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                var isLeftMode = localSettings.Values["NavViewIsLeftMode"] as bool? ?? true;

                NavView.PaneDisplayMode = isLeftMode ? 
                    NavigationViewPaneDisplayMode.Auto : 
                    NavigationViewPaneDisplayMode.Top;

                if (!isLeftMode)
                {
                    NavView.IsPaneOpen = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading navigation position: {ex.Message}");
                NavView.PaneDisplayMode = NavigationViewPaneDisplayMode.Auto;
            }
        }

        /// <summary>
        /// Saves the current NavigationView position to settings
        /// </summary>
        public void SaveNavigationViewPosition(bool isLeftMode)
        {
            try
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values["NavViewIsLeftMode"] = isLeftMode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving navigation position: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the NavigationView display mode
        /// </summary>
        public void UpdateNavigationViewMode(bool isLeftMode)
        {
            try
            {
                if (isLeftMode)
                {
                    NavView.PaneDisplayMode = NavigationViewPaneDisplayMode.Auto;
                    NavView.IsPaneOpen = true;
                }
                else
                {
                    NavView.PaneDisplayMode = NavigationViewPaneDisplayMode.Top;
                    NavView.IsPaneOpen = false;
                }

                SaveNavigationViewPosition(isLeftMode);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating navigation mode: {ex.Message}");
            }
        }

        #endregion
    }
}