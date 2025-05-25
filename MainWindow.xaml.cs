using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;
using WinRT.Interop;
using Plantilla.Pages.About;
using Plantilla.Pages.Processes;
using Plantilla.Pages.Settings;

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
    }
}