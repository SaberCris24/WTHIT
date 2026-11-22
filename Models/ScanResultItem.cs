using System;
using System.Collections.ObjectModel;
using WTHIT.Core;

namespace WTHIT.Models
{
    public class ScanResultItem : ObservableObject, IDisposable
    {
        private string _scanResult = string.Empty;
        private bool _isScanned;
        private string _filePath = string.Empty;
        private VirusTotalScanResult? _scanDetails;
        private bool _disposed;

        public string ProcessName { get; set; } = string.Empty;

        public string ScanResult
        {
            get => _scanResult;
            set { if (!_disposed) SetProperty(ref _scanResult, value); }
        }

        public bool IsScanned { get => _isScanned; set => SetProperty(ref _isScanned, value); }
        public string FilePath { get => _filePath; set => SetProperty(ref _filePath, value); }
        public VirusTotalScanResult? ScanDetails { get => _scanDetails; set => SetProperty(ref _scanDetails, value); }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }

    public static class ScanResultsManager
    {
        public static ObservableCollection<ScanResultItem> ScanResults { get; } = new();
        private static readonly object _lock = new();

        public static void AddScanResult(ScanResultItem result)
        {
            lock (_lock)
            {
                ScanResults.Insert(0, result);
                int limit = Configuration.MaxScanResults;
                while (ScanResults.Count > limit)
                {
                    var old = ScanResults[ScanResults.Count - 1];
                    ScanResults.RemoveAt(ScanResults.Count - 1);
                    old.Dispose();
                }
            }
        }

        public static void Clear()
        {
            lock (_lock)
            {
                foreach (var item in ScanResults)
                {
                    item.Dispose();
                }
                ScanResults.Clear();
            }
        }

        public static void RemoveScanResult(ScanResultItem result)
        {
            lock (_lock)
            {
                ScanResults.Remove(result);
                result.Dispose();
            }
        }
    }
}
