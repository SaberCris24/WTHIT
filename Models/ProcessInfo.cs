using SQLite;

namespace Plantilla.Models
{
    [Table("ProcessInfo")]
    public class ProcessInfo
    {
        [PrimaryKey]
        public string ProcessName { get; set; }

        public string ApplicationRelated { get; set; }

        public string FileLocation { get; set; }

        public string Description { get; set; }

        public string IsCpuIntensive { get; set; }
    }
}