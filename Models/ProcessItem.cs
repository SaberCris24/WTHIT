using System.Collections.Concurrent;
using System.ComponentModel;

namespace Plantilla.Models
{
    /// <summary>
    /// Represents a process with properties for name, ID, application relation, virus status, and selection state
    /// Implements INotifyPropertyChanged to support data binding
    /// </summary>
    public class ProcessItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        private string _virusStatus = "Not Scanned";

        /// <summary>
        /// Gets or sets the name of the process
        /// </summary>
        public string ProcessName { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the ID of the process
        /// </summary>
        public int ProcessId { get; set; }
        
        /// <summary>
        /// Gets or sets the application related to this process
        /// </summary>
        public string ApplicationRelated { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the virus scan status of the process
        /// Notifies listeners when the value changes
        /// </summary>
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

        /// <summary>
        /// Gets or sets the information text for the process
        /// </summary>
        public string Information { get; set; } = "Click to view details";

        /// <summary>
        /// Gets or sets whether the process is selected
        /// Notifies listeners when the value changes
        /// </summary>
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

        /// <summary>
        /// Event that is triggered when a property value changes
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raises the PropertyChanged event for the specified property
        /// </summary>
        /// <param name="propertyName">Name of the property that changed</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Resets the process item to its default state
        /// </summary>
        internal void Reset()
        {
            ProcessName = string.Empty;
            ProcessId = 0;
            ApplicationRelated = string.Empty;
            _virusStatus = "Not Scanned";
            Information = "Click to view details";
            _isSelected = false;
        }

        /// <summary>
        /// Copies property values from another process item
        /// </summary>
        /// <param name="other">The process item to copy from</param>
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

    /// <summary>
    /// Provides a pool of ProcessItem objects to reduce memory allocations
    /// Uses a concurrent queue for thread-safe operations
    /// </summary>
    public static class ProcessItemPool
    {
        private static readonly ConcurrentQueue<ProcessItem> _pool = new();
        private const int MaxPoolSize = 200;
        private static int _currentPoolSize = 0;

        /// <summary>
        /// Gets a ProcessItem from the pool or creates a new one if the pool is empty
        /// </summary>
        /// <returns>A ProcessItem instance</returns>
        public static ProcessItem Rent()
        {
            if (_pool.TryDequeue(out var item))
            {
                System.Threading.Interlocked.Decrement(ref _currentPoolSize);
                return item;
            }
            return new ProcessItem();
        }

        /// <summary>
        /// Returns a ProcessItem to the pool for reuse
        /// </summary>
        /// <param name="item">The ProcessItem to return to the pool</param>
        public static void Return(ProcessItem item)
        {
            if (item == null || _currentPoolSize >= MaxPoolSize) return;

            item.Reset();
            _pool.Enqueue(item);
            System.Threading.Interlocked.Increment(ref _currentPoolSize);
        }

        /// <summary>
        /// Returns multiple ProcessItems to the pool
        /// </summary>
        /// <param name="items">Collection of ProcessItems to return</param>
        public static void ReturnRange(System.Collections.Generic.IEnumerable<ProcessItem> items)
        {
            if (items == null) return;

            foreach (var item in items)
            {
                Return(item);
            }
        }

        /// <summary>
        /// Clears all items from the pool
        /// </summary>
        public static void Clear()
        {
            while (_pool.TryDequeue(out _))
            {
                System.Threading.Interlocked.Decrement(ref _currentPoolSize);
            }
        }

        /// <summary>
        /// Gets statistics about the pool
        /// </summary>
        /// <returns>A tuple containing the current pool size and maximum pool size</returns>
        public static (int PoolSize, int MaxSize) GetPoolStats()
        {
            return (_currentPoolSize, MaxPoolSize);
        }
    }
}