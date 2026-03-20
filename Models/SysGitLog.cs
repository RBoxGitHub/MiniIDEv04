using SQLite;

namespace MiniIDEv04.Models
{
    [Table("sys_GitLog")]
    public class SysGitLog
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [MaxLength(500)]
        public string CommitMessage { get; set; } = string.Empty;

        [MaxLength(50)]
        public string VersionTag { get; set; } = string.Empty;

        public DateTime PushedAt { get; set; } = DateTime.UtcNow;

        public bool Success { get; set; } = false;

        /// <summary>Full git output captured from stdout/stderr</summary>
        public string GitOutput { get; set; } = string.Empty;

        /// <summary>Display string for the Success flag — used in Git Log tab.</summary>
        [Ignore]
        public string SuccessDisplay => Success ? "✅" : "❌";
    }
}
