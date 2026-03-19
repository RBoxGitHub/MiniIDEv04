using SQLite;

namespace MiniIDEv04.Models
{
    [Table("sys_Libraries")]
    public class SysLibrary
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// Unique key for this library.
        /// e.g. "WPF_Builtin", "Telerik_WPF", "MyCustomControls"
        /// </summary>
        [MaxLength(100), Unique]
        public string LibraryKey { get; set; } = string.Empty;

        /// <summary>
        /// Display name shown in the library manager.
        /// </summary>
        [MaxLength(200)]
        public string LibraryName { get; set; } = string.Empty;

        /// <summary>
        /// Full path to the assembly DLL on disk.
        /// Empty for built-in WPF (PresentationFramework is always available).
        /// </summary>
        [MaxLength(500)]
        public string AssemblyPath { get; set; } = string.Empty;

        /// <summary>
        /// Assembly name — used by reflection scanner (Item 4) to load the assembly.
        /// e.g. "PresentationFramework", "Telerik.Windows.Controls"
        /// </summary>
        [MaxLength(300)]
        public string AssemblyName { get; set; } = string.Empty;

        /// <summary>
        /// Version string of the library.
        /// </summary>
        [MaxLength(50)]
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Source tier: 0 = built-in, 1 = reflection-scanned, 2 = DLL drop-in.
        /// Tier 2 libraries are added via the DLL drop-in folder watcher (Item 7).
        /// </summary>
        public int Tier { get; set; } = 0;

        /// <summary>
        /// When true, LibraryScanner (Item 4) will scan this assembly for controls.
        /// </summary>
        public bool IsScanEnabled { get; set; } = false;

        /// <summary>
        /// When true, this library is loaded into the XamlRenderer at runtime.
        /// Phase 3: RoslynCompiler uses this to build dynamic assembly refs.
        /// </summary>
        public bool IsLoaded { get; set; } = false;

        /// <summary>
        /// When false the library is hidden from the UI but retained in DB.
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// Last time LibraryScanner successfully scanned this assembly.
        /// Null = never scanned.
        /// </summary>
        public DateTime? LastScannedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
