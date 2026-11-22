using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using WTHIT.Controls;
using WTHIT.Models;

namespace WTHIT.Services;

public sealed class ScanResultDialogService
{
    public async Task ShowAsync(ScanResultItem scanResult, XamlRoot xamlRoot)
    {
        if (scanResult == null || xamlRoot == null) return;

        try
        {
            var reportView = new ScanReportView();
            reportView.PopulateFromScanResult(scanResult);

            // Use the active window theme for the dialog.
            var theme = xamlRoot.Content is FrameworkElement rootElement
                ? rootElement.ActualTheme
                : ElementTheme.Default;

            var dialog = new ContentDialog
            {
                XamlRoot = xamlRoot,
                Title = "VirusTotal Report",
                Content = reportView,
                CloseButtonText = "Close",
                DefaultButton = ContentDialogButton.Close,
                RequestedTheme = theme,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                MaxWidth = xamlRoot.Size.Width - 48,
                MaxHeight = xamlRoot.Size.Height - 48
            };

            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to show scan result dialog: " + ex.Message, ex);
        }
    }
}
