using SQLite;

namespace Plantilla.Models
{
    /// <summary>
    /// Represents a process information entity mapped to the "ProcessInfo" table in the database.
    /// </summary>
    [Table("ProcessInfo")]
    public class ProcessInfo
    {
        /// <summary>
        /// Gets or sets the name of the process. This property serves as the primary key.
        /// </summary>
        [PrimaryKey]
        public string ProcessName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the application this process is associated with.
        /// </summary>
        public string ApplicationRelated { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the file path of the process executable.
        /// </summary>
        public string FileLocation { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a description of the process.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the process is typically CPU-intensive. Stored as a string.
        /// </summary>
        public string IsCpuIntensive { get; set; } = string.Empty;
    }
}