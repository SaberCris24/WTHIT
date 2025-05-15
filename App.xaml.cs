using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using DevWinUI;

namespace Plantilla
{
    public partial class App : Application
    {
        public IThemeService ThemeService { get; set; }
        public static Window MainWindow { get; private set; }
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        
        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            MainWindow = new MainWindow(1000, 400);
            MainWindow.ExtendsContentIntoTitleBar = true;
            MainWindow.Activate();

            ThemeService = new ThemeService();
            ThemeService.Initialize(MainWindow).ConfigureElementTheme(ElementTheme.Default);
            ThemeService.ConfigureBackdrop(BackdropType.Mica);

        }
    }
}