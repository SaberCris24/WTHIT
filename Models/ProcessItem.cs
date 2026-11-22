using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using WTHIT.Services;

namespace WTHIT.Models
{
    /// <summary>Represents a running process or a group of process instances.</summary>
    public class ProcessItem : INotifyPropertyChanged, IDisposable
    {
        private PropertyChangedEventHandler? _propertyChanged;
        private bool _disposed;
        private bool _isLoadingIcon;

        private string _processName = string.Empty;
        private int _processId;
        private string _applicationRelated = string.Empty;
        private string _virusStatus = string.Empty;
        private string _information = "Click to view details";
        private bool _isSelected;
        private bool _isScanned;
        private string _filePath = string.Empty;
        private VirusTotalScanResult? _scanDetails;
        private BitmapImage? _processIcon;

        public string ProcessName { get => _processName; set => SetProperty(ref _processName, value); }
        public int ProcessId { get => _processId; set => SetProperty(ref _processId, value); }
        public string ApplicationRelated { get => _applicationRelated; set => SetProperty(ref _applicationRelated, value); }
        public string VirusStatus { get => _virusStatus; set => SetProperty(ref _virusStatus, value); }
        public string Information { get => _information; set => SetProperty(ref _information, value); }
        public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
        public bool IsScanned { get => _isScanned; set => SetProperty(ref _isScanned, value); }
        public string FilePath { get => _filePath; set => SetProperty(ref _filePath, value); }
        public VirusTotalScanResult? ScanDetails { get => _scanDetails; set => SetProperty(ref _scanDetails, value); }
        public BitmapImage? ProcessIcon
        {
            get => _processIcon;
            set
            {
                if (SetProperty(ref _processIcon, value))
                    OnPropertyChanged(nameof(IconPlaceholderVisibility));
            }
        }

        public Visibility IconPlaceholderVisibility => ProcessIcon == null ? Visibility.Visible : Visibility.Collapsed;

        private int _instanceCount = 1;
        public int InstanceCount
        {
            get => _instanceCount;
            set
            {
                if (SetProperty(ref _instanceCount, value))
                {
                    OnPropertyChanged(nameof(ChevronVisibility));
                    OnPropertyChanged(nameof(BadgeVisibility));
                }
            }
        }

        private bool _isGroupExpanded;
        public bool IsGroupExpanded
        {
            get => _isGroupExpanded;
            set { if (SetProperty(ref _isGroupExpanded, value)) OnPropertyChanged(nameof(ChevronGlyph)); }
        }

        private bool _isChildRow;
        public bool IsChildRow
        {
            get => _isChildRow;
            set { if (SetProperty(ref _isChildRow, value)) OnPropertyChanged(nameof(RowIndent)); }
        }

        private string _groupCaption = "Process";
        public string GroupCaption { get => _groupCaption; set => SetProperty(ref _groupCaption, value); }

        public Visibility ChevronVisibility => InstanceCount > 1 ? Visibility.Visible : Visibility.Collapsed;
        public Visibility BadgeVisibility => InstanceCount > 1 ? Visibility.Visible : Visibility.Collapsed;
        public string ChevronGlyph => IsGroupExpanded ? "\uE70D" : "\uE76C";
        public Thickness RowIndent => IsChildRow ? new Thickness(18, 0, 0, 0) : new Thickness(0);

        public event PropertyChangedEventHandler? PropertyChanged
        {
            add => _propertyChanged += value;
            remove => _propertyChanged -= value;
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            _propertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>Loads the process icon on the UI thread.</summary>
        public async Task LoadIconAsync()
        {
            if (_disposed || _isLoadingIcon || ProcessIcon != null)
                return;

            _isLoadingIcon = true;
            try
            {
                var icon = await ProcessIconService.GetIconForProcessAsync(ProcessId, FilePath);
                if (!_disposed)
                    ProcessIcon = icon;
            }
            finally
            {
                _isLoadingIcon = false;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _processIcon = null;
            _scanDetails = null;
            _propertyChanged = null;
            GC.SuppressFinalize(this);
        }
    }
}