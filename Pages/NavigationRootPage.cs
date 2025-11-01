using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Plantilla.Pages
{
    /// <summary>
    /// Singleton class that manages the root navigation view for the application
    /// </summary>
    public class NavigationRootPage
    {
        /// <summary>
        /// The main navigation view control for the application
        /// </summary>
        private static NavigationView _navigationView;
        
        /// <summary>
        /// The singleton instance of the NavigationRootPage
        /// </summary>
        private static NavigationRootPage _instance;

        /// <summary>
        /// Gets the navigation view control
        /// </summary>
        public NavigationView NavigationView => _navigationView;

        /// <summary>
        /// Private constructor to enforce singleton pattern
        /// </summary>
        private NavigationRootPage() { }

        /// <summary>
        /// Gets the singleton instance of NavigationRootPage for the given UI element
        /// </summary>
        /// <param name="element">The UI element requesting the instance</param>
        /// <returns>The singleton instance of NavigationRootPage</returns>
        public static NavigationRootPage GetForElement(UIElement element)
        {
            if (_instance == null)
            {
                _instance = new NavigationRootPage();
            }
            return _instance;
        }

        /// <summary>
        /// Initializes the NavigationRootPage with the specified navigation view
        /// </summary>
        /// <param name="navigationView">The navigation view to use as the root navigation</param>
        /// <exception cref="ArgumentNullException">Thrown when navigationView is null</exception>
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