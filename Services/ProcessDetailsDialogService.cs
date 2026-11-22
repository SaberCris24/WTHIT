using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using WTHIT.Controls;
using WTHIT.Models;

namespace WTHIT.Services;

public enum ProcessStatus { Running, Ended, SystemProtected }

public sealed class ProcessDetailsDialogService : IDisposable
{
    private readonly DatabaseService _databaseService;
    private readonly Dictionary<string, ProcessInfo?> _cache = new(StringComparer.OrdinalIgnoreCase);
    private bool _isShowing;
    private bool _disposed;
    private ContentDialog? _dialog;
    private CancellationTokenSource? _dialogCancellation;

    public ProcessDetailsDialogService(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task ShowAsync(ProcessItem process, XamlRoot xamlRoot, Func<ProcessItem, CancellationToken, Task>? onScanRequested = null)
    {
        if (_disposed || process == null || xamlRoot == null || _isShowing) return;
        _isShowing = true;
        using var cancellation = new CancellationTokenSource();
        _dialogCancellation = cancellation;
        try
        {
            var infoTask = GetOrFetchInfoAsync(process.ProcessName);
            var runtimeTask = Task.Run(() => GetRuntimeDetails(process.ProcessId));
            await Task.WhenAll(infoTask, runtimeTask);
            if (cancellation.IsCancellationRequested) return;

            var processInfo = infoTask.Result;
            var (path, description, status) = runtimeTask.Result;

            var detailsView = CreateDetailsView(process, processInfo, path, description, status);
            var content = CreateTabbedContent(detailsView, () => CreateScanView(process, onScanRequested, cancellation.Token));

            var theme = xamlRoot.Content is FrameworkElement rootElement ? rootElement.ActualTheme : ElementTheme.Default;
            var dialog = new ContentDialog
            {
                XamlRoot = xamlRoot,
                Title = process.ProcessName,
                Content = content,
                CloseButtonText = "Close",
                DefaultButton = ContentDialogButton.Close,
                RequestedTheme = theme,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                MaxWidth = xamlRoot.Size.Width - 48,
                MaxHeight = xamlRoot.Size.Height - 48
            };
            _dialog = dialog;
            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ProcessDetails] Error: {ex.Message}");
        }
        finally
        {
            _dialog = null;
            _dialogCancellation = null;
            cancellation.Cancel();
            _isShowing = false;
        }
    }

    #region Tabs
    private FrameworkElement CreateTabbedContent(FrameworkElement detailsView, Func<FrameworkElement> scanViewFactory)
    {
        var detailsTab = CreateTab("Details", "\uE946");
        var scanTab = CreateTab("Scan Report", "\uEA18");
        var tabStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
        tabStack.Children.Add(detailsTab);
        tabStack.Children.Add(scanTab);

        var tabStrip = new Border
        {
            Child = tabStack,
            Style = Application.Current.Resources["CardBorderStyle"] as Style,
            HorizontalAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(3),
            CornerRadius = new CornerRadius(8),
            Margin = new Thickness(0, 0, 0, 12)
        };

        var host = new Grid();
        host.Children.Add(detailsView);

        FrameworkElement? scanView = null;
        void Select(int index)
        {
            ApplyTabState(detailsTab, index == 0);
            ApplyTabState(scanTab, index == 1);
            detailsView.Visibility = index == 0 ? Visibility.Visible : Visibility.Collapsed;
            if (index == 1 && scanView == null)
            {
                scanView = scanViewFactory();
                host.Children.Add(scanView);
            }
            if (scanView != null)
                scanView.Visibility = index == 1 ? Visibility.Visible : Visibility.Collapsed;
        }
        detailsTab.Click += (_, _) => Select(0);
        scanTab.Click += (_, _) => Select(1);
        Select(0);

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(tabStrip, 0);
        Grid.SetRow(host, 1);
        root.Children.Add(tabStrip);
        root.Children.Add(host);

        return root;
    }

    private static Button CreateTab(string text, string glyph)
    {
        var content = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center
        };

        var icon = new FontIcon
        {
            Glyph = glyph,
            FontSize = 14,
            VerticalAlignment = VerticalAlignment.Center
        };

        var label = new TextBlock
        {
            Text = text,
            VerticalAlignment = VerticalAlignment.Center
        };

        content.Children.Add(icon);
        content.Children.Add(label);

        return new Button
        {
            Content = content,
            MinWidth = 120,
            Padding = new Thickness(16, 8, 16, 8),
            CornerRadius = new CornerRadius(6),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
    }

    private static void ApplyTabState(Button tab, bool selected)
    {
        if (selected)
        {
            tab.Style = Application.Current.Resources["AccentButtonStyle"] as Style;
            tab.ClearValue(Control.BackgroundProperty);
            tab.ClearValue(Control.BorderBrushProperty);
        }
        else
        {
            tab.Style = Application.Current.Resources["DefaultButtonStyle"] as Style;
            var transparent = new SolidColorBrush(Colors.Transparent);
            tab.Background = transparent;
            tab.BorderBrush = transparent;
        }
    }
    #endregion

    #region Views
    private FrameworkElement CreateDetailsView(ProcessItem process, ProcessInfo? info, string path, string desc, ProcessStatus status)
    {
        var panel = new StackPanel { Spacing = 12 };

        var hero = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16 };
        FrameworkElement icon = process.ProcessIcon != null
            ? new Image { Source = process.ProcessIcon, Width = 48, Height = 48, VerticalAlignment = VerticalAlignment.Center }
            : new FontIcon { Glyph = "\uE8A5", FontSize = 48, VerticalAlignment = VerticalAlignment.Center };

        var textStack = new StackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        textStack.Children.Add(new TextBlock
        {
            Text = process.ProcessName,
            Style = Application.Current.Resources["TitleTextBlockStyle"] as Style,
            VerticalAlignment = VerticalAlignment.Center
        });

        hero.Children.Add(icon);
        hero.Children.Add(textStack);
        panel.Children.Add(hero);

        var resolvedPath = (status != ProcessStatus.Running && !string.IsNullOrWhiteSpace(process.FilePath))
            ? process.FilePath
            : path;

        panel.Children.Add(CreateInfoCard("General Information", "\uE946",
            ("Process ID", process.ProcessId.ToString()),
            ("Path", resolvedPath),
            ("Description", desc)));

        panel.Children.Add(CreateInfoCard("Process Information", "\uE8A5",
            ("Status", status.ToString()),
            ("Scan Status", string.IsNullOrWhiteSpace(process.VirusStatus) ? "Not Scanned" : process.VirusStatus),
            ("Related Application", string.IsNullOrWhiteSpace(process.ApplicationRelated) ? "N/A" : process.ApplicationRelated)));

        if (info == null)
        {
            var dbStack = new StackPanel { Spacing = 8 };
            var dbHeader = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
            dbHeader.Children.Add(new FontIcon
            {
                Glyph = "\uEE94",
                FontSize = 16,
                Foreground = Application.Current.Resources["AccentFillColorDefaultBrush"] as Brush,
                VerticalAlignment = VerticalAlignment.Center
            });
            dbHeader.Children.Add(new TextBlock
            {
                Text = "Database Information",
                Style = Application.Current.Resources["SubtitleTextBlockStyle"] as Style
            });
            dbStack.Children.Add(dbHeader);
            dbStack.Children.Add(new TextBlock
            {
                Text = "This process is not registered in the database.",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                Opacity = 0.7
            });
            panel.Children.Add(new Border { Child = dbStack, Style = Application.Current.Resources["CardBorderStyle"] as Style });
        }
        else
        {
            panel.Children.Add(CreateInfoCard("Database Information", "\uEE94",
                ("Application", info.ApplicationRelated ?? "N/A"),
                ("Description", info.Description ?? "N/A"),
                ("Resource Intensive", info.IsCpuIntensive ?? "N/A")));
        }

        return new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
    }

    private Border CreateInfoCard(string title, string iconGlyph, params (string Label, string Value)[] fields)
    {
        var stack = new StackPanel { Spacing = 8 };

        var header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
        header.Children.Add(new FontIcon
        {
            Glyph = iconGlyph,
            FontSize = 16,
            Foreground = Application.Current.Resources["AccentFillColorDefaultBrush"] as Brush,
            VerticalAlignment = VerticalAlignment.Center
        });
        header.Children.Add(new TextBlock
        {
            Text = title,
            Style = Application.Current.Resources["SubtitleTextBlockStyle"] as Style
        });
        stack.Children.Add(header);

        foreach (var (label, value) in fields)
        {
            var row = new Grid { ColumnSpacing = 8 };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var lbl = new TextBlock { Text = label, FontWeight = FontWeights.SemiBold, FontSize = 12, TextWrapping = TextWrapping.Wrap };
            var val = new TextBlock { Text = value, TextWrapping = TextWrapping.Wrap, FontSize = 12 };

            Grid.SetColumn(val, 1);
            row.Children.Add(lbl);
            row.Children.Add(val);
            stack.Children.Add(row);
        }

        return new Border { Child = stack, Style = Application.Current.Resources["CardBorderStyle"] as Style };
    }

    private FrameworkElement CreateScanView(ProcessItem process, Func<ProcessItem, CancellationToken, Task>? onScanRequested, CancellationToken cancellationToken)
    {
        var host = new Grid();

        void Populate()
        {
            host.Children.Clear();
            host.Children.Add(process.ScanDetails is not { HasReport: true }
                ? CreateNotScannedPlaceholder(process, onScanRequested, Populate, cancellationToken)
                : CreateScanReport(process));
        }

        Populate();
        return host;
    }

    private FrameworkElement CreateNotScannedPlaceholder(ProcessItem process, Func<ProcessItem, CancellationToken, Task>? onScanRequested, Action refresh, CancellationToken cancellationToken)
    {
        var emptyStack = new StackPanel
        {
            Spacing = 16,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 48, 0, 0)
        };
        emptyStack.Children.Add(new FontIcon
        {
            Glyph = "\uE946",
            FontSize = 48,
            Opacity = 0.6
        });
        emptyStack.Children.Add(new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(process.VirusStatus) || process.VirusStatus == "Not Scanned"
                ? "No VirusTotal report has been retrieved for this process."
                : process.VirusStatus,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        });

        if (onScanRequested != null)
        {
            var btn = new Button
            {
                Content = "Scan Now",
                HorizontalAlignment = HorizontalAlignment.Center,
                Style = Application.Current.Resources["AccentButtonStyle"] as Style
            };
            btn.Click += async (s, e) =>
            {
                btn.IsEnabled = false;
                try
                {
                    await onScanRequested(process, cancellationToken);
                    if (!cancellationToken.IsCancellationRequested)
                        refresh();
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ProcessDetails] Scan from dialog failed: {ex.Message}");
                }
                finally
                {
                    if (!cancellationToken.IsCancellationRequested)
                        btn.IsEnabled = true;
                }
            };
            emptyStack.Children.Add(btn);
        }
        return emptyStack;
    }

    private FrameworkElement CreateScanReport(ProcessItem process)
    {
        var reportView = new ScanReportView();

        UIElement processIcon = process.ProcessIcon != null
            ? new Image { Source = process.ProcessIcon, Width = 48, Height = 48, VerticalAlignment = VerticalAlignment.Center }
            : new FontIcon { Glyph = "\uE8A5", FontSize = 48, Foreground = Application.Current.Resources["AccentFillColorDefaultBrush"] as Brush, VerticalAlignment = VerticalAlignment.Center };

        reportView.ProcessIconElement = processIcon;

        var scanItem = new WTHIT.Models.ScanResultItem
        {
            ProcessName = process.ProcessName,
            ScanResult = process.VirusStatus,
            ScanDetails = process.ScanDetails
        };
        reportView.PopulateFromScanResult(scanItem);
        return reportView;
    }
    #endregion

    #region Helpers & Cache
    private async Task<ProcessInfo?> GetOrFetchInfoAsync(string processName)
    {
        if (_cache.TryGetValue(processName, out var cached))
            return cached;

        var info = await _databaseService.GetProcessInfoAsync(processName);
        if (!_disposed)
        {
            if (_cache.Count >= 100)
                _cache.Clear();

            _cache[processName] = info;
        }

        return info;
    }

    private static (string Path, string Desc, ProcessStatus Status) GetRuntimeDetails(int pid)
    {
        try
        {
            using var proc = Process.GetProcessById(pid);
            var mod = proc.MainModule;
            return (mod?.FileName ?? "N/A", mod?.FileVersionInfo?.FileDescription ?? "N/A", ProcessStatus.Running);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return ("Access Denied", "System Protected", ProcessStatus.SystemProtected);
        }
        catch { return ("Ended", "Process not found", ProcessStatus.Ended); }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _dialogCancellation?.Cancel();
        _dialog?.Hide();
        _cache.Clear();
    }
    #endregion
}
