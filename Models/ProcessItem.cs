using System.Collections.Concurrent;
using System.ComponentModel;

namespace Plantilla.Models
{
    public class ProcessItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        private string _virusStatus = "Not Scanned";

        public string ProcessName { get; set; } = string.Empty;
        public int ProcessId { get; set; }
        public string ApplicationRelated { get; set; } = string.Empty;
        
        public string VirusStatus
        {
            get => _virusStatus;
            set
            {
                if (_virusStatus != value)
                {
                    _virusStatus = value;
                    OnPropertyChanged(nameof(VirusStatus));
                }
            }
        }

        public string Information { get; set; } = "Click to view details";

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        internal void Reset()
        {
            ProcessName = string.Empty;
            ProcessId = 0;
            ApplicationRelated = string.Empty;
            _virusStatus = "Not Scanned";
            Information = "Click to view details";
            _isSelected = false;
        }

        internal void CopyFrom(ProcessItem other)
        {
            if (other == null) return;

            ProcessName = other.ProcessName;
            ProcessId = other.ProcessId;
            ApplicationRelated = other.ApplicationRelated;
            VirusStatus = other.VirusStatus;
            Information = other.Information;
            IsSelected = other.IsSelected;
        }
    }

    public static class ProcessItemPool
    {
        private static readonly ConcurrentQueue<ProcessItem> _pool = new();
        private const int MaxPoolSize = 200;
        private static int _currentPoolSize = 0;

        public static ProcessItem Rent()
        {
            if (_pool.TryDequeue(out var item))
            {
                System.Threading.Interlocked.Decrement(ref _currentPoolSize);
                return item;
            }
            return new ProcessItem();
        }

        public static void Return(ProcessItem item)
        {
            if (item == null || _currentPoolSize >= MaxPoolSize) return;

            item.Reset();
            _pool.Enqueue(item);
            System.Threading.Interlocked.Increment(ref _currentPoolSize);
        }

        public static void ReturnRange(System.Collections.Generic.IEnumerable<ProcessItem> items)
        {
            if (items == null) return;

            foreach (var item in items)
            {
                Return(item);
            }
        }

        public static void Clear()
        {
            while (_pool.TryDequeue(out _))
            {
                System.Threading.Interlocked.Decrement(ref _currentPoolSize);
            }
        }

        public static (int PoolSize, int MaxSize) GetPoolStats()
        {
            return (_currentPoolSize, MaxPoolSize);
        }
    }
}