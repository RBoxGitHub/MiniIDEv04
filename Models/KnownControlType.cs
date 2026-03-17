namespace MiniIDEv04.Models
{
    /// <summary>
    /// In-memory record describing a known property for a given control type.
    /// Used to populate the property picker dropdown in SysManagerWindow.
    /// Phase 2: LibraryScanner will auto-populate these via reflection.
    /// Phase 1 seed: hand-authored for WPF + Telerik common controls.
    /// </summary>
    public record KnownControlProperty(
        string PropertyName,
        string PropertyType,   // "string" | "double" | "int" | "bool" | "color" | "thickness"
        string Category,       // "Layout" | "Visual" | "Content" | "Binding"
        string DefaultValue,
        string? Description = null
    );

    /// <summary>
    /// In-memory record describing a known control type available for placement.
    /// Seeded manually for Phase 1; LibraryScanner populates this in Phase 2.
    /// </summary>
    public record KnownControlType(
        string FullTypeName,       // "System.Windows.Controls.Button"
        string ShortName,          // "Button"
        string AssemblySource,     // "WPF" | "Telerik"
        string Category,           // "Basic" | "Input" | "Layout" | "Data" | "Telerik"
        IReadOnlyList<KnownControlProperty> Properties
    );
}
