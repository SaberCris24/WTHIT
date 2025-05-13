using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Plantilla
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
   public sealed partial class MainWindow : Window
    {
        private List<string> sugerencias = new List<string>();

        public MainWindow()
        {
            this.InitializeComponent();

            AppWindow.Title = "WTHIT";

            // Set the window size (including borders)
            AppWindow.Resize(new Windows.Graphics.SizeInt32(800, 500));

            // Set the window position on screen
            AppWindow.Move(new Windows.Graphics.PointInt32(50, 50));

            // Set the preferred theme for the title bar
            AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;

            // Set the taskbar icon (displayed in the taskbar)
            AppWindow.SetTaskbarIcon("Assets/Tiles/GalleryIcon.ico");

            // Set the title bar icon (displayed in the window's title bar)
            AppWindow.SetTitleBarIcon("Assets/Tiles/GalleryIcon.ico");
            
            sugerencias = new List<string>
            {
                "explorer.exe",
                "msedge.exe",
                "notepad.exe",
            };
        }

        private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {

            if (args.QueryText != string.Empty)
            {

            }
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var filteredItems = string.IsNullOrEmpty(sender.Text) ? 
                    new string[] { } : 
                    sugerencias
                        .Where(p => p.ToLower().Contains(sender.Text.ToLower()))
                        .ToArray();
                
                sender.ItemsSource = filteredItems;
            }
        }

        private void SearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            sender.Text = args.SelectedItem.ToString();
        }
    }
}