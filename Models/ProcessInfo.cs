using SQLite;

namespace Plantilla.Models
{
    [Table("ProcessInfo")]
    public class ProcessInfo
    {
        [PrimaryKey]
        public string ProcessName { get; set; } = string.Empty;

        public string ApplicationRelated { get; set; } = string.Empty;

        public string FileLocation { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string IsCpuIntensive { get; set; } = string.Empty;
    }
}