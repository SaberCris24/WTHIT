using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Plantilla.Pages
{
    public class NavigationRootPage
    {
        private static NavigationView _navigationView;
        private static NavigationRootPage _instance;

        public NavigationView NavigationView => _navigationView;

        private NavigationRootPage() { }

        public static NavigationRootPage GetForElement(UIElement element)
        {
            if (_instance == null)
            {
                _instance = new NavigationRootPage();
            }
            return _instance;
        }

        public static void Initialize(NavigationView navigationView)
        {
            if (navigationView == null)
                throw new ArgumentNullException(nameof(navigationView));

            _navigationView = navigationView;
            if (_instance == null)
            {
                _instance = new NavigationRootPage();
            }
        }
    }
}