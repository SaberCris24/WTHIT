namespace Plantilla.Models
{
    public class ProcessItem : System.ComponentModel.INotifyPropertyChanged
    {
        private bool _isSelected;
        private string _virusStatus;

        public string ProcessName { get; set; }
        public int ProcessId { get; set; }
        public string ApplicationRelated { get; set; }
        
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
        
        public string Information { get; set; }
        
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

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }
}