using SQLite;

namespace MiniIDEv04.Models
{
    [Table("sys_ToolboxEntries")]
    public class SysToolboxEntry
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// Foreign key → sys_ToolboxGroups.GroupKey
        /// </summary>
        [MaxLength(100), Indexed]
        public string GroupKey { get; set; } = string.Empty;

        /// <summary>
        /// Display name shown in the toolbox list.
        /// e.g. "Button", "TextBox", "DataGrid"
        /// </summary>
        [MaxLength(150)]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Fully qualified type name used to instantiate the control.
        /// e.g. "System.Windows.Controls.Button"
        /// Phase 4: XamlRenderer uses this to emit the correct element tag.
        /// </summary>
        [MaxLength(300)]
        public string TypeFullName { get; set; } = string.Empty;

        /// <summary>
        /// Assembly name where this control lives.
        /// e.g. "PresentationFramework" for built-in WPF controls.
        /// Phase 4 / Tier 2: DLL name for drop-in assemblies.
        /// </summary>
        [MaxLength(300)]
        public string AssemblyName { get; set; } = string.Empty;

        /// <summary>
        /// XAML namespace prefix for this control.
        /// e.g. "http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        /// Phase 3: XmlnsInjector uses this to build the xmlns map.
        /// </summary>
        [MaxLength(300)]
        public string XmlNamespace { get; set; } = string.Empty;

        /// <summary>
        /// Default XAML snippet inserted when dragging onto the canvas.
        /// e.g. "&lt;Button Content=\"Button\" Width=\"80\" Height=\"30\"/&gt;"
        /// </summary>
        public string DefaultXamlSnippet { get; set; } = string.Empty;

        /// <summary>
        /// Optional icon glyph or emoji shown next to the entry.
        /// </summary>
        [MaxLength(10)]
        public string Icon { get; set; } = string.Empty;

        /// <summary>
        /// Controls display order within the group.
        /// </summary>
        public int SortOrder { get; set; } = 0;

        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// Source tier: 0 = seeded/built-in, 1 = reflection-scanned, 2 = DLL drop-in.
        /// </summary>
        public int Tier { get; set; } = 0;

        /// <summary>
        /// FK → sys_Libraries.Id — null for built-in WPF controls.
        /// </summary>
        public int? LibraryId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
