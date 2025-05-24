using Microsoft.UI.Xaml.Controls;
using System;

namespace Plantilla.Services
{
    public interface INotificationService
    {
        void Initialize(InfoBar infoBar);
        void ShowError(string message);
        void ShowSuccess(string message);
        void ShowWarning(string message);
        void ShowInformation(string message);
        void Clear();
    }

    public class NotificationService : INotificationService
    {
        private InfoBar? _infoBar;

        public void Initialize(InfoBar infoBar)
        {
            _infoBar = infoBar ?? throw new ArgumentNullException(nameof(infoBar));
        }

        public void ShowError(string message)
        {
            if (_infoBar == null) throw new InvalidOperationException("NotificationService not initialized");
            
            _infoBar.Message = message;
            _infoBar.Severity = InfoBarSeverity.Error;
            _infoBar.IsOpen = true;
        }

        public void ShowSuccess(string message)
        {
            if (_infoBar == null) throw new InvalidOperationException("NotificationService not initialized");
            
            _infoBar.Message = message;
            _infoBar.Severity = InfoBarSeverity.Success;
            _infoBar.IsOpen = true;
        }

        public void ShowWarning(string message)
        {
            if (_infoBar == null) throw new InvalidOperationException("NotificationService not initialized");
            
            _infoBar.Message = message;
            _infoBar.Severity = InfoBarSeverity.Warning;
            _infoBar.IsOpen = true;
        }

        public void ShowInformation(string message)
        {
            if (_infoBar == null) throw new InvalidOperationException("NotificationService not initialized");
            
            _infoBar.Message = message;
            _infoBar.Severity = InfoBarSeverity.Informational;
            _infoBar.IsOpen = true;
        }

        public void Clear()
        {
            if (_infoBar == null) throw new InvalidOperationException("NotificationService not initialized");
            
            _infoBar.IsOpen = false;
            _infoBar.Message = string.Empty;
        }
    }
}