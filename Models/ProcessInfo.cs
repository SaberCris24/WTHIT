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
        
        public bool IsCpuIntensive { get; set; }
    }

    [Table("VersionInfo")]
    public class VersionInfo
    {
        [PrimaryKey]
        public string Key { get; set; }
        public int Version { get; set; }
    }
}