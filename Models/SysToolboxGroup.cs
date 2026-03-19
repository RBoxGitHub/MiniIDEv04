using SQLite;

namespace MiniIDEv04.Models
{
    [Table("sys_ToolboxGroups")]
    public class SysToolboxGroup
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// Unique key for this group — used by ToolboxRegistry lookups.
        /// e.g. "WPF_Layout", "WPF_Input", "WPF_Display"
        /// </summary>
        [MaxLength(100), Unique]
        public string GroupKey { get; set; } = string.Empty;

        /// <summary>
        /// Display name shown in the RadPanelBar header.
        /// </summary>
        [MaxLength(150)]
        public string GroupName { get; set; } = string.Empty;

        /// <summary>
        /// Optional icon glyph or emoji shown next to the group name.
        /// </summary>
        [MaxLength(10)]
        public string Icon { get; set; } = string.Empty;

        /// <summary>
        /// Controls display order in the toolbox panel bar.
        /// </summary>
        public int SortOrder { get; set; } = 0;

        /// <summary>
        /// When false, the group is hidden from the toolbox UI.
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// Source tier: 0 = seeded/built-in, 1 = reflection-scanned, 2 = DLL drop-in.
        /// Used by LibraryScanner (Item 4) to identify scan origin.
        /// </summary>
        public int Tier { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
