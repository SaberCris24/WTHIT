using System;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Controls;

namespace WTHIT.Services
{
    public enum NotificationSeverity
    {
        Info,
        Success,
        Warning,
        Error
    }

    /// <summary>Queues transient notifications in the main window's overlay.</summary>
    public sealed class NotificationService
    {
        public static NotificationService Instance { get; } = new();

        private readonly ObservableCollection<NotificationItem> _notifications = new();
        private WeakReference<ItemsControl>? _host;
        private ItemsControl? Host => _host != null && _host.TryGetTarget(out var host) ? host : null;

        private string? _lastMessage;
        private NotificationSeverity _lastSeverity;
        private DateTimeOffset _lastShownAt;

        private const int MaxConcurrent = 3;
        private const int MaxMessageLength = 500;
        private static readonly TimeSpan DuplicateInterval = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan DefaultDuration = TimeSpan.FromSeconds(4);
        private static readonly TimeSpan ErrorDuration = TimeSpan.FromSeconds(7);

        private NotificationService() { }

        /// <summary>Attaches the notification queue for the lifetime of the window.</summary>
        public void Initialize(ItemsControl host)
        {
            if (Host is { } previousHost)
            {
                if (ReferenceEquals(previousHost, host))
                    return;

                Detach(previousHost);
            }

            _host = new WeakReference<ItemsControl>(host);
            host.ItemsSource = _notifications;
        }

        /// <summary>Releases the window host and all active notifications.</summary>
        public void Detach(ItemsControl host)
        {
            if (!ReferenceEquals(Host, host))
                return;

            _host = null;
            host.ItemsSource = null;
            _notifications.Clear();
            _lastMessage = null;
        }

        /// <summary>Shows a notification and suppresses immediate repeats of the same message.</summary>
        public void Show(string message, NotificationSeverity severity = NotificationSeverity.Info, TimeSpan? duration = null)
        {
            if (string.IsNullOrWhiteSpace(message) || Host is not { } host)
                return;

            message = message.Trim();
            if (message.Length > MaxMessageLength)
                message = message[..MaxMessageLength] + "…";

            var lifetime = duration ?? (severity == NotificationSeverity.Error ? ErrorDuration : DefaultDuration);
            if (lifetime <= TimeSpan.Zero)
                lifetime = DefaultDuration;

            host.DispatcherQueue.TryEnqueue(() =>
            {
                if (!ReferenceEquals(Host, host))
                    return;

                var now = DateTimeOffset.UtcNow;
                if (message == _lastMessage && severity == _lastSeverity && now - _lastShownAt < DuplicateInterval)
                    return;

                _lastMessage = message;
                _lastSeverity = severity;
                _lastShownAt = now;

                if (_notifications.Count == MaxConcurrent)
                    _notifications.RemoveAt(0);

                _notifications.Add(new NotificationItem(message, severity, lifetime));
            });
        }

        public void ShowSuccess(string message, TimeSpan? duration = null) => Show(message, NotificationSeverity.Success, duration);
        public void ShowWarning(string message, TimeSpan? duration = null) => Show(message, NotificationSeverity.Warning, duration);
        public void ShowError(string message, TimeSpan? duration = null) => Show(message, NotificationSeverity.Error, duration);
        public void ShowInfo(string message, TimeSpan? duration = null) => Show(message, NotificationSeverity.Info, duration);

        internal void Remove(NotificationItem item) => _notifications.Remove(item);
    }

    /// <summary>Contains the text, severity, and display duration of a notification.</summary>
    public sealed class NotificationItem(string message, NotificationSeverity severity, TimeSpan duration)
    {
        public string Message { get; } = message;
        public NotificationSeverity Severity { get; } = severity;
        public TimeSpan Duration { get; } = duration;
    }
}
