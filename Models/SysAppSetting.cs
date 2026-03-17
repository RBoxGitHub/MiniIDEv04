using SQLite;

namespace MiniIDEv04.Models
{
    [Table("sys_AppSettings")]
    public class SysAppSetting
    {
        [PrimaryKey, MaxLength(150)]
        public string Key { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Value { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
