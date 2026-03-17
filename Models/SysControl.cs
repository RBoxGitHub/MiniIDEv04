using SQLite;

namespace MiniIDEv04.Models
{
    /// <summary>
    /// One row = one control instance placed in a parent view or panel.
    /// Runtime engine reads this table and instantiates the control via reflection.
    /// </summary>
    [Table("sys_Controls")]
    public class SysControl
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>Unique key — e.g. "btn_SaveProject_MainWindow"</summary>
        [MaxLength(150), Unique]
        public string ControlKey { get; set; } = string.Empty;

        /// <summary>Friendly display name shown in SysManagerWindow</summary>
        [MaxLength(150)]
        public string ControlName { get; set; } = string.Empty;

        /// <summary>
        /// Fully qualified CLR type.
        /// e.g. "System.Windows.Controls.Button"
        ///      "Telerik.Windows.Controls.RadButton"
        /// </summary>
        [MaxLength(512)]
        public string ControlType { get; set; } = string.Empty;

        /// <summary>
        /// PanelKey from sys_Panels OR view name — where this control lives.
        /// e.g. "QuickAddPanel", "MainWindow", "ThingsToDoWindow"
        /// </summary>
        [MaxLength(150)]
        public string ParentKey { get; set; } = string.Empty;

        /// <summary>"Panel" | "View" | "Canvas" | "Grid"</summary>
        [MaxLength(50)]
        public string ParentType { get; set; } = "Panel";

        /// <summary>Assembly source — "WPF" | "Telerik" | dll path for custom</summary>
        [MaxLength(512)]
        public string AssemblySource { get; set; } = "WPF";

        // ── Layout ────────────────────────────────────────────────────────
        public double? LayoutLeft   { get; set; }
        public double? LayoutTop    { get; set; }
        public int?    LayoutRow    { get; set; }
        public int?    LayoutColumn { get; set; }
        public double? Width        { get; set; }
        public double? Height       { get; set; }

        // ── State ─────────────────────────────────────────────────────────
        public bool IsVisible { get; set; } = true;
        public bool IsEnabled { get; set; } = true;
        public int  SortOrder { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
