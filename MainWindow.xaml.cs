using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;
using WinRT.Interop;
using Plantilla.Pages.About;
using Plantilla.Pages.Processes;
using Plantilla.Pages.Settings;
using Windows.Storage;

namespace Plantilla
{
    /// <summary>
    /// Main window class that handles the application's primary window functionality
    /// </summary>
    public sealed partial class MainWindow : Window
    {
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

        /// <summary>
        /// Public property to access the NavigationView control from other parts of the app
        /// </summary>
        public NavigationView NavigationViewControl => NavView;

        /// <summary>
        /// Constructor for the main window
        /// </summary>
        /// <param name="MinWidth">Minimum width of the window</param>
        /// <param name="MinHeight">Minimum height of the window</param>
        public MainWindow(int MinWidth, int MinHeight)
        {
            // Initialize the window components
            this.InitializeComponent();
            
            // Enable custom title bar
            this.ExtendsContentIntoTitleBar = true;

            // Set the window title
            AppWindow.Title = "WTHIT";

            // Set the window icon
            var hwnd = WindowNative.GetWindowHandle(this);
            string iconPath = System.IO.Path.Combine(Environment.CurrentDirectory, "Assets", "icon.ico");
            // Load the icon from file
            IntPtr hIcon = LoadImage(IntPtr.Zero, iconPath, IMAGE_ICON, 0, 0, LR_LOADFROMFILE);
            // Set both large and small icons for the window
            SendMessage(hwnd, WM_SETICON, (IntPtr)1, hIcon); // ICON_BIG
            SendMessage(hwnd, WM_SETICON, (IntPtr)0, hIcon); // ICON_SMALL

            // Create and set window presenter with minimum size constraints
            OverlappedPresenter presenter = OverlappedPresenter.Create();
            presenter.PreferredMinimumWidth = MinWidth;
            presenter.PreferredMinimumHeight = MinHeight;
            AppWindow.SetPresenter(presenter);

            // Set the initial page to Processes
            contentFrame.Navigate(typeof(ProcessesPage));

            // Initialize NavigationView position from settings
            LoadNavigationViewPosition();

            // Subscribe to DisplayMode changes to save the state
            NavView.DisplayModeChanged += NavView_DisplayModeChanged;
        }

        /// <summary>
        /// Event handler for navigation view selection changes
        /// </summary>
        /// <param name="sender">The navigation view that triggered the event</param>
        /// <param name="args">Event arguments containing selection information</param>
        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            // Handle navigation to settings page
            if (args.IsSettingsSelected)
            {
                contentFrame.Navigate(typeof(SettingsPage));
            }
            // Handle navigation to other pages
            else if (args.SelectedItem is NavigationViewItem selectedItem)
            {
                // Navigate based on the selected item's tag
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

                // Update additional properties based on mode
                if (!isLeftMode)
                {
                    NavView.IsPaneOpen = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading navigation position: {ex.Message}");
                // Default to left mode if there's an error
                NavView.PaneDisplayMode = NavigationViewPaneDisplayMode.Auto;
            }
        }

        /// <summary>
        /// Saves the current NavigationView position to settings
        /// </summary>
        /// <param name="isLeftMode">True if the navigation is in left mode, false for top mode</param>
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
        /// <param name="isLeftMode">True to set left mode, false for top mode</param>
        public void UpdateNavigationViewMode(bool isLeftMode)
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
    }
}