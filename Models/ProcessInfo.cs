using SQLite;

namespace WTHIT.Models
{
    /// <summary>
    /// Process information entity mapped to the ProcessInfo table
    /// </summary>
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
