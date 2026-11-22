using System;
using System.Diagnostics;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;
using WTHIT.Services;

namespace WTHIT.Controls
{
    /// <summary>Displays a transient notification and pauses dismissal during interaction.</summary>
    public sealed partial class NotificationControl : UserControl
    {
        private readonly DispatcherQueueTimer _timer;
        private readonly Stopwatch _elapsed = new();
        private TimeSpan _remaining;
        private bool _pointerOver;
        private bool _hasFocus;

        public static readonly DependencyProperty ItemProperty =
            DependencyProperty.Register(nameof(Item), typeof(NotificationItem), typeof(NotificationControl),
                new PropertyMetadata(null, OnItemChanged));

        public NotificationItem? Item
        {
            get => (NotificationItem?)GetValue(ItemProperty);
            set => SetValue(ItemProperty, value);
        }

        public NotificationControl()
        {
            InitializeComponent();

            _timer = DispatcherQueue.CreateTimer();
            _timer.IsRepeating = false;
            _timer.Tick += OnTimerElapsed;

            Loaded += (_, _) => ShowItem();
            Unloaded += (_, _) =>
            {
                _timer.Stop();
                _elapsed.Reset();
                _pointerOver = _hasFocus = false;
            };
            PointerEntered += (_, _) =>
            {
                _pointerOver = true;
                PauseCountdown();
            };
            PointerExited += (_, _) =>
            {
                _pointerOver = false;
                ResumeCountdown();
            };
            GotFocus += (_, _) =>
            {
                _hasFocus = true;
                PauseCountdown();
            };
            LosingFocus += OnLosingFocus;
        }

        private static void OnItemChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            if (sender is NotificationControl { IsLoaded: true } control)
                control.ShowItem();
        }

        /// <summary>Updates the visual state and schedules dismissal with a single timer callback.</summary>
        private void ShowItem()
        {
            _timer.Stop();
            _elapsed.Reset();
            if (Item is not { } item)
                return;

            _remaining = item.Duration;
            TitleText.Text = item.Severity switch
            {
                NotificationSeverity.Success => "Success",
                NotificationSeverity.Warning => "Warning",
                NotificationSeverity.Error => "Error",
                _ => "Information"
            };
            MessageText.Text = item.Message;
            VisualStateManager.GoToState(this, item.Severity.ToString(), false);

            CopyButton.Visibility = item.Severity == NotificationSeverity.Error ? Visibility.Visible : Visibility.Collapsed;
            CopyIcon.Glyph = "\uE8C8";
            ToolTipService.SetToolTip(CopyButton, "Copy details");
            ToolTipService.SetToolTip(MessageText, new TextBlock { Text = item.Message, TextWrapping = TextWrapping.Wrap, MaxWidth = 320 });

            FrameworkElementAutomationPeer.CreatePeerForElement(MessageText)?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
            ResumeCountdown();
        }

        private void OnLosingFocus(UIElement sender, LosingFocusEventArgs args)
        {
            for (var element = args.NewFocusedElement; element != null; element = VisualTreeHelper.GetParent(element))
            {
                if (ReferenceEquals(this, element))
                    return;
            }

            _hasFocus = false;
            ResumeCountdown();
        }

        private void PauseCountdown()
        {
            if (!_elapsed.IsRunning)
                return;

            _timer.Stop();
            _remaining -= _elapsed.Elapsed;
            _elapsed.Reset();
        }

        private void ResumeCountdown()
        {
            if (_pointerOver || _hasFocus || _elapsed.IsRunning || !IsLoaded || Item == null)
                return;

            _timer.Interval = _remaining > TimeSpan.Zero ? _remaining : TimeSpan.FromMilliseconds(1);
            _elapsed.Restart();
            _timer.Start();
        }

        private void OnTimerElapsed(DispatcherQueueTimer sender, object args)
        {
            _elapsed.Reset();
            if (!_pointerOver && !_hasFocus)
                Dismiss();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs args) => Dismiss();

        private void Dismiss()
        {
            _timer.Stop();
            _elapsed.Reset();
            if (Item is { } item)
                NotificationService.Instance.Remove(item);
        }

        private void CopyButton_Click(object sender, RoutedEventArgs args)
        {
            if (Item is not { } item)
                return;

            try
            {
                var data = new DataPackage();
                data.SetText(item.Message);
                Clipboard.SetContent(data);

                CopyIcon.Glyph = "\uE73E";
                ToolTipService.SetToolTip(CopyButton, "Copied");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to copy notification details: {ex.Message}");
            }
        }
    }
}
