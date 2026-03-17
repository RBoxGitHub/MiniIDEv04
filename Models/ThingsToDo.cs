using SQLite;

namespace MiniIDEv04.Models
{
    [Table("sys_ThingsToDo")]
    public class ThingsToDo
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public bool IsComplete { get; set; } = false;

        [MaxLength(4000)]
        public string? SideNote { get; set; }

        /// <summary>Instance / service name — e.g. "LibraryScanner"</summary>
        [MaxLength(256)]
        public string? ReferenceObject { get; set; }

        /// <summary>Fully qualified CLR type — e.g. "MiniIDEv04.Services.LibraryScanner"</summary>
        [MaxLength(512)]
        public string? TypeReference { get; set; }

        /// <summary>Source file name — e.g. "LibraryScanner.cs"</summary>
        [MaxLength(256)]
        public string? FileName { get; set; }

        /// <summary>Relative or absolute file path</summary>
        [MaxLength(1024)]
        public string? FilePath { get; set; }

        /// <summary>Build phase 1-4</summary>
        public int Phase { get; set; } = 1;

        /// <summary>1=High 2=Normal 3=Low</summary>
        public int Priority { get; set; } = 2;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
