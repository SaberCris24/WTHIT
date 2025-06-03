namespace Plantilla.Models
{
    /// <summary>
    /// Model class for representing process information with change notification
    /// </summary>
    public class ProcessItem : System.ComponentModel.INotifyPropertyChanged
    {
        // Private fields for properties with change notification
        private bool _isSelected;
        private string _virusStatus = string.Empty;

        public string ProcessName { get; set; } = string.Empty; 
        public int ProcessId { get; set; }
        public string ApplicationRelated { get; set; } = string.Empty; 
        /// <summary>
        /// Current virus scan status
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

        public string Information { get; set; } = string.Empty;

        /// <summary>
        /// Selection state of the process item
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
        /// Property change notification event
        /// </summary>
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raises the PropertyChanged event
        /// </summary>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }
}