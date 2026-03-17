using SQLite;
using MiniIDEv04.Models;
using System.IO;

namespace MiniIDEv04.Data
{
    public static class ProjectDatabase
    {
        private static string? _dbPath;

        public static string DbPath
        {
            get
            {
                if (_dbPath is null)
                {
                    var notesDir = Path.Combine(AppContext.BaseDirectory, "ProjectNotes");
                    Directory.CreateDirectory(notesDir);
                    _dbPath = Path.Combine(notesDir, "miniIDE.db");
                }
                return _dbPath;
            }
        }

        public static void Initialize()
        {
            using var db = GetConnection();

            db.CreateTable<ThingsToDo>();
            db.CreateTable<SysPanel>();
            db.CreateTable<SysAppSetting>();
            db.CreateTable<SysControl>();
            db.CreateTable<SysControlProperty>();

            SeedAppSettings(db);
            SeedPanels(db);
            SeedThingsToDo(db);
        }

        // ── Seeds ──────────────────────────────────────────────────────────

        private static void SeedAppSettings(SQLiteConnection db)
        {
            if (db.Table<SysAppSetting>().Count() > 0) return;

            db.InsertAll(new[]
            {
                new SysAppSetting
                {
                    Key         = "AppVersion",
                    Value       = "MiniIDEv04",
                    Description = "Current IDE version identifier"
                },
                new SysAppSetting
                {
                    Key         = "DefaultTheme",
                    Value       = "Windows11",
                    Description = "Telerik theme applied at startup"
                },
                new SysAppSetting
                {
                    Key         = "ProjectNotesPath",
                    Value       = "ProjectNotes",
                    Description = "Relative path to the local SQLite DB folder"
                },
                new SysAppSetting
                {
                    Key         = "SqlServerConnection",
                    Value       = "",
                    Description = "SQL Server connection string — Phase 4 exported .sln projects only"
                }
            });
        }

        private static void SeedPanels(SQLiteConnection db)
        {
            if (db.Table<SysPanel>().Count() > 0) return;

            db.InsertAll(new[]
            {
                new SysPanel
                {
                    PanelKey      = "QuickAddPanel",
                    PanelName     = "📝 Quick Add Note",
                    Description   = "Floating quick-add bar — type a note and hit + Add",
                    IsVisible     = true,
                    IsPinned      = false,
                    PosLeft       = 20,
                    PosTop        = 0,
                    PanelWidth    = 620,
                    PanelHeight   = 56,
                    TitleBarColor = "#FF37474F",
                    LaunchTarget  = "",
                    HasSaveButton = false,
                    SortOrder     = 2
                },
                new SysPanel
                {
                    PanelKey      = "SysManagerLauncher",
                    PanelName     = "📝 Notes Launcher",
                    Description   = "Floating wrench launcher — double-click to open ThingsToDo modal",
                    IsVisible     = true,
                    IsPinned      = false,
                    PosLeft       = 20,
                    PosTop        = 0,
                    PanelWidth    = 80,
                    PanelHeight   = 80,
                    TitleBarColor = "#FF37474F",
                    LaunchTarget  = "ThingsToDoWindow",
                    HasSaveButton = true,
                    SortOrder     = 0
                },
                new SysPanel
                {
                    PanelKey      = "SysManagerPanel",
                    PanelName     = "⚙ Sys Manager",
                    Description   = "Pinned sys_ launcher — double-click to open SysManager modal",
                    IsVisible     = true,
                    IsPinned      = true,
                    PosLeft       = 20,
                    PosTop        = 700,
                    PanelWidth    = 120,
                    PanelHeight   = 80,
                    TitleBarColor = "#FF1A237E",
                    LaunchTarget  = "SysManagerWindow",
                    HasSaveButton = true,
                    SortOrder     = 1
                }
            });
        }

        private static void SeedThingsToDo(SQLiteConnection db)
        {
            if (db.Table<ThingsToDo>().Count() > 0) return;

            db.InsertAll(new[]
            {
                new ThingsToDo
                {
                    SideNote        = "MiniIDEv04 initialized. sys_ThingsToDo, sys_Panels, sys_AppSettings created. DraggablePanel wired. Repository interfaces in place.",
                    ReferenceObject = "ProjectDatabase",
                    TypeReference   = "MiniIDEv04.Data.ProjectDatabase",
                    FileName        = "ProjectDatabase.cs",
                    FilePath        = "Data/ProjectDatabase.cs",
                    Phase = 1, Priority = 3, IsComplete = true
                },
                new ThingsToDo
                {
                    SideNote        = "Phase 2: Build sys_ToolboxGroups, sys_ToolboxEntries, sys_Libraries. LibraryScanner (Tier 1 reflection), ToolboxRegistry, RadPanelBar wired, DLL drop-in folder watcher (Tier 2).",
                    ReferenceObject = "LibraryScanner",
                    TypeReference   = "MiniIDEv04.Services.LibraryScanner",
                    FileName        = "LibraryScanner.cs",
                    FilePath        = "Services/LibraryScanner.cs",
                    Phase = 2, Priority = 1
                },
                new ThingsToDo
                {
                    SideNote        = "Phase 3: XamlRenderer (XamlReader.Load two-pass), RoslynCompiler (CSharpCompilation + dynamic assembly refs), XmlnsInjector, Tier 3 live control registration.",
                    ReferenceObject = "RoslynCompiler",
                    TypeReference   = "MiniIDEv04.Services.RoslynCompiler",
                    FileName        = "RoslynCompiler.cs",
                    FilePath        = "Services/RoslynCompiler.cs",
                    Phase = 3, Priority = 1
                },
                new ThingsToDo
                {
                    SideNote        = "Phase 4: ProjectSerializer (.wpfproj JSON zip), SlnGenerator (.sln + .csproj templates), Dapper/SQL Server layer for exported project DBs only.",
                    ReferenceObject = "SlnGenerator",
                    TypeReference   = "MiniIDEv04.Services.SlnGenerator",
                    FileName        = "SlnGenerator.cs",
                    FilePath        = "Services/SlnGenerator.cs",
                    Phase = 4, Priority = 1
                },
                new ThingsToDo
                {
                    SideNote        = "DraggablePanel — clean from scratch in MiniIDEv04.Controls. Title bar drag, resize grip, ShowCloseButton, ShowResizeGrip DPs. PositionChanged fires on mouse release only — one DB write per drag.",
                    ReferenceObject = "DraggablePanel",
                    TypeReference   = "MiniIDEv04.Controls.DraggablePanel",
                    FileName        = "DraggablePanel.xaml",
                    FilePath        = "Controls/DraggablePanel.xaml",
                    Phase = 1, Priority = 1
                },
                new ThingsToDo
                {
                    SideNote        = "sys_Panels drives ALL DraggablePanel instances. PanelManagerService watches rows — IsVisible, PosLeft, PosTop, Width, Height, TitleBarColor all reflect live on screen. Single source of truth for layout. No layout.json needed.",
                    ReferenceObject = "PanelManagerService",
                    TypeReference   = "MiniIDEv04.Services.PanelManagerService",
                    FileName        = "PanelManagerService.cs",
                    FilePath        = "Services/PanelManagerService.cs",
                    Phase = 1, Priority = 1
                },
                new ThingsToDo
                {
                    SideNote        = "Thumb.DragCompleted handler needed — save panel size to sys_Panels on resize end. Currently resize does not auto-persist. Add in Phase 2.",
                    ReferenceObject = "DraggablePanel",
                    TypeReference   = "MiniIDEv04.Controls.DraggablePanel",
                    FileName        = "DraggablePanel.xaml.cs",
                    FilePath        = "Controls/DraggablePanel.xaml.cs",
                    Phase = 2, Priority = 2
                },
                new ThingsToDo
                {
                    SideNote        = "Dynamic panel spawn — instantiate DraggablePanel at runtime for cloned panels. PanelManagerService.CloneAsync() inserts the DB row but the Canvas child must be created programmatically in MainWindow. Wire in Phase 2.",
                    ReferenceObject = "PanelManagerService",
                    TypeReference   = "MiniIDEv04.Services.PanelManagerService",
                    FileName        = "PanelManagerService.cs",
                    FilePath        = "Services/PanelManagerService.cs",
                    Phase = 2, Priority = 2
                }
            });
        }

        // ── Connection factories ───────────────────────────────────────────

        public static SQLiteConnection      GetConnection()      => new(DbPath);
        public static SQLiteAsyncConnection GetAsyncConnection() => new(DbPath);

        // ── Runtime path switching ─────────────────────────────────────────

        /// <summary>Point the IDE at a different .db file at runtime.</summary>
        public static void SetDbPath(string path)
        {
            _dbPath = path;
        }

        /// <summary>Revert to the default ProjectNotes/miniIDE.db path.</summary>
        public static void ResetDbPath()
        {
            _dbPath = null; // forces DbPath getter to recalculate default
        }
    }
}
