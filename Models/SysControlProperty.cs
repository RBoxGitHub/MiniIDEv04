using SQLite;

namespace MiniIDEv04.Models
{
    /// <summary>
    /// Child table of sys_Controls.
    /// One row per property on a control instance.
    /// e.g. ControlKey="btn_Save", PropertyName="Background", PropertyValue="#FF90EE90", PropertyType="string"
    /// </summary>
    [Table("sys_ControlProperties")]
    public class SysControlProperty
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>FK to sys_Controls.ControlKey</summary>
        [MaxLength(150), Indexed]
        public string ControlKey { get; set; } = string.Empty;

        /// <summary>
        /// Property name — matches WPF DependencyProperty or CLR property name.
        /// Attached properties use dot notation: "Grid.Row", "Canvas.Left"
        /// </summary>
        [MaxLength(150)]
        public string PropertyName { get; set; } = string.Empty;

        /// <summary>String representation of the value — runtime converts based on PropertyType</summary>
        [MaxLength(1000)]
        public string PropertyValue { get; set; } = string.Empty;

        /// <summary>
        /// How the runtime should interpret PropertyValue:
        /// "string" | "double" | "int" | "bool" | "color" | "thickness" | "binding"
        /// </summary>
        [MaxLength(50)]
        public string PropertyType { get; set; } = "string";

        /// <summary>Property category for grouping in the picker UI</summary>
        [MaxLength(50)]
        public string Category { get; set; } = "Content";

        /// <summary>Order within the control's property list</summary>
        public int SortOrder { get; set; } = 0;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
