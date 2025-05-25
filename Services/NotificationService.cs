using Microsoft.UI.Xaml.Controls;
using System;

namespace Plantilla.Services
{
    /// <summary>
    /// Interface for managing notifications in the application
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Sets up the InfoBar control for notifications
        /// </summary>
        void Initialize(InfoBar infoBar);

        /// <summary>
        /// Shows an error notification
        /// </summary>
        void ShowError(string message);

        /// <summary>
        /// Shows a success notification
        /// </summary>
        void ShowSuccess(string message);

        /// <summary>
        /// Shows a warning notification
        /// </summary>
        void ShowWarning(string message);

        /// <summary>
        /// Shows an information notification
        /// </summary>
        void ShowInformation(string message);

        /// <summary>
        /// Clears the current notification
        /// </summary>
        void Clear();
    }

    /// <summary>
    /// Service for displaying notifications using InfoBar
    /// </summary>
    public class NotificationService : INotificationService
    {
        // InfoBar control reference
        private InfoBar? _infoBar;

        /// <summary>
        /// Sets up the InfoBar for notifications
        /// </summary>
        public void Initialize(InfoBar infoBar)
        {
            _infoBar = infoBar ?? throw new ArgumentNullException(nameof(infoBar));
        }

        /// <summary>
        /// Displays an error message
        /// </summary>
        public void ShowError(string message)
        {
            if (_infoBar == null) throw new InvalidOperationException("NotificationService not initialized");
            
            _infoBar.Message = message;
            _infoBar.Severity = InfoBarSeverity.Error;
            _infoBar.IsOpen = true;
        }

        /// <summary>
        /// Displays a success message
        /// </summary>
        public void ShowSuccess(string message)
        {
            if (_infoBar == null) throw new InvalidOperationException("NotificationService not initialized");
            
            _infoBar.Message = message;
            _infoBar.Severity = InfoBarSeverity.Success;
            _infoBar.IsOpen = true;
        }

        /// <summary>
        /// Displays a warning message
        /// </summary>
        public void ShowWarning(string message)
        {
            if (_infoBar == null) throw new InvalidOperationException("NotificationService not initialized");
            
            _infoBar.Message = message;
            _infoBar.Severity = InfoBarSeverity.Warning;
            _infoBar.IsOpen = true;
        }

        /// <summary>
        /// Displays an information message
        /// </summary>
        public void ShowInformation(string message)
        {
            if (_infoBar == null) throw new InvalidOperationException("NotificationService not initialized");
            
            _infoBar.Message = message;
            _infoBar.Severity = InfoBarSeverity.Informational;
            _infoBar.IsOpen = true;
        }

        /// <summary>
        /// Clears and hides the notification
        /// </summary>
        public void Clear()
        {
            if (_infoBar == null) throw new InvalidOperationException("NotificationService not initialized");
            
            _infoBar.IsOpen = false;
            _infoBar.Message = string.Empty;
        }
    }
}