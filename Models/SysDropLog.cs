using SQLite;

namespace MiniIDEv04.Models
{
    [Table("sys_DropLog")]
    public class SysDropLog
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [MaxLength(260)]
        public string ZipFileName { get; set; } = string.Empty;

        [MaxLength(260)]
        public string FileName { get; set; } = string.Empty;

        [MaxLength(260)]
        public string Destination { get; set; } = string.Empty;

        /// <summary>New | Updated | Skipped | Error</summary>
        [MaxLength(20)]
        public string Status { get; set; } = string.Empty;

        public DateTime DroppedAt { get; set; } = DateTime.UtcNow;
    }
}
