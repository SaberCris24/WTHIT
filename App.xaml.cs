using Microsoft.UI.Xaml;
using DevWinUI;

namespace Plantilla
{
    public partial class App : Application
    {
        public IThemeService ThemeService { get; private set; } = null!;
        public static Window MainWindow { get; private set; } = null!; 
        
        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            MainWindow = new MainWindow(800, 400);
            MainWindow.ExtendsContentIntoTitleBar = true;
            MainWindow.Activate();

            ThemeService = new ThemeService();
            ThemeService.Initialize(MainWindow).ConfigureElementTheme(ElementTheme.Default);
            ThemeService.ConfigureBackdrop(BackdropType.Mica);
        }
    }
}