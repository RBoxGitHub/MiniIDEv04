using SQLite;

namespace MiniIDEv04.Models
{
    [Table("sys_Panels")]
    public class SysPanel
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [MaxLength(100), Unique]
        public string PanelKey { get; set; } = string.Empty;

        [MaxLength(150)]
        public string PanelName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsVisible  { get; set; } = true;
        public bool IsPinned   { get; set; } = false;
        public bool IsCloned   { get; set; } = false;

        [MaxLength(100)]
        public string? ClonedFromKey { get; set; }

        public double PosLeft    { get; set; } = 20;
        public double PosTop     { get; set; } = 20;
        public double PanelWidth  { get; set; } = 400;
        public double PanelHeight { get; set; } = 300;

        /// <summary>
        /// When true, the panel UI should show a Save Changes button.
        /// Controlled via sys_Panels — no code change needed to toggle.
        /// </summary>
        public bool HasSaveButton { get; set; } = false;

        [MaxLength(20)]
        public string TitleBarColor { get; set; } = "#FF37474F";

        /// <summary>
        /// Window to open on double-click — matched in PanelManagerService.
        /// e.g. "ThingsToDoWindow", "SysManagerWindow", or empty = no action.
        /// </summary>
        [MaxLength(100)]
        public string? LaunchTarget { get; set; }

        [MaxLength(20)]
        public string Version { get; set; } = "1.0";

        public int SortOrder { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
