using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Plantilla
{
    public sealed partial class SettingsPage : Page
    {
        private MainWindow? parentWindow;
        
        public SettingsPage()
        {
            this.InitializeComponent();
        }
        
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            // Guardar referencia a la ventana principal
            if (e.Parameter is MainWindow window)
            {
                parentWindow = window;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // Volver a la interfaz principal usando el método de MainWindow
            parentWindow?.ReturnToMainInterface();
        }
    }
}