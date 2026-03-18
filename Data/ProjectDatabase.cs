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
            db.CreateTable<SysGitLog>();
            db.CreateTable<SysDropLog>();

            // ── Migrations — safe to run on existing DB ──────────────────
            MigrateAddColumnIfMissing(db, "sys_Panels", "ControlClass",  "TEXT DEFAULT ''");
            MigrateAddColumnIfMissing(db, "sys_Panels", "HasSaveButton", "INTEGER DEFAULT 0");
            MigrateAddColumnIfMissing(db, "sys_Panels", "LaunchTarget",  "TEXT DEFAULT ''");

            SeedAppSettings(db);
            SeedPanels(db);
            SeedThingsToDo(db);

            // ── Back-fill ControlClass for existing rows ─────────────────
            BackFillControlClass(db);
        }

        private static void MigrateAddColumnIfMissing(SQLiteConnection db,
            string table, string column, string definition)
        {
            try
            {
                db.Execute($"ALTER TABLE {table} ADD COLUMN {column} {definition}");
            }
            catch
            {
                // Column already exists — swallow the error, continue
            }
        }

        private static void BackFillControlClass(SQLiteConnection db)
        {
            var map = new Dictionary<string, string>
            {
                ["QuickAddPanel"]      = "QuickAddPanelControl",
                ["SysManagerLauncher"] = "SysManagerLauncherControl",
                ["SysManagerPanel"]    = "SysManagerPanelControl",
                ["GitHubLauncher"]     = "GitHubLauncherControl",
                ["DropZoneLauncher"]   = "DropZoneLauncherControl",
            };

            foreach (var (key, cls) in map)
                db.Execute(
                    "UPDATE sys_Panels SET ControlClass=? WHERE PanelKey=? AND (ControlClass IS NULL OR ControlClass='')",
                    cls, key);

            // Insert DropZoneLauncher if missing
            var existingDz = db.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM sys_Panels WHERE PanelKey='DropZoneLauncher'");
            if (existingDz == 0)
                db.Execute(@"
                    INSERT INTO sys_Panels
                        (PanelKey, PanelName, Description, IsVisible, IsPinned, IsCloned,
                         PosLeft, PosTop, PanelWidth, PanelHeight, TitleBarColor,
                         LaunchTarget, ControlClass, HasSaveButton, Version, SortOrder,
                         CreatedAt, UpdatedAt)
                    VALUES
                        ('DropZoneLauncher','📦 Drop Zone','Launcher — double-click to open Drop Zone',
                         1,0,0, 210,0,80,80,'#FF4A148C',
                         'DropZoneWindow','DropZoneLauncherControl',0,'4.0.0',5,
                         datetime('now'),datetime('now'))");

            // Insert GitHubLauncher if missing (replaces old GitHubPushPanel)
            var existing = db.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM sys_Panels WHERE PanelKey='GitHubLauncher'");
            if (existing == 0)
                db.Execute(@"
                    INSERT INTO sys_Panels
                        (PanelKey, PanelName, Description, IsVisible, IsPinned, IsCloned,
                         PosLeft, PosTop, PanelWidth, PanelHeight, TitleBarColor,
                         LaunchTarget, ControlClass, HasSaveButton, Version, SortOrder,
                         CreatedAt, UpdatedAt)
                    VALUES
                        ('GitHubLauncher','🐙 GitHub','Launcher — double-click to open GitHub Push modal',
                         1,0,0, 120,0,80,80,'#FF1B5E20',
                         'GitHubPushWindow','GitHubLauncherControl',0,'4.0.0',4,
                         datetime('now'),datetime('now'))");

            // Remove old GitHubPushPanel if present
            db.Execute("DELETE FROM sys_Panels WHERE PanelKey='GitHubPushPanel'");

            // Fix GitHubPushPanel position if it was inserted at the old overlapping position
            db.Execute(
                "UPDATE sys_Panels SET PosLeft=660, PosTop=0 WHERE PanelKey='GitHubPushPanel' AND PosLeft=20 AND PosTop=100");
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
                },
                new SysAppSetting
                {
                    Key         = "ProjectRootPath",
                    Value       = @"D:\GrokCryptoTrack\Production-Claude\MiniIDE-WorkFolder\MiniIDEv04",
                    Description = "Root folder of the MiniIDEv04 project source"
                },
                new SysAppSetting
                {
                    Key         = "ZipWorkFolder",
                    Value       = "ZipDrop",
                    Description = "Relative path to zip drop folder — resolved from AppContext.BaseDirectory"
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
                    PosLeft       = 20, PosTop = 0,
                    PanelWidth    = 620, PanelHeight = 56,
                    TitleBarColor = "#FF37474F",
                    LaunchTarget  = "",
                    ControlClass  = "QuickAddPanelControl",
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
                    PosLeft       = 20, PosTop = 0,
                    PanelWidth    = 80, PanelHeight = 80,
                    TitleBarColor = "#FF37474F",
                    LaunchTarget  = "ThingsToDoWindow",
                    ControlClass  = "SysManagerLauncherControl",
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
                    PosLeft       = 20, PosTop = 700,
                    PanelWidth    = 120, PanelHeight = 80,
                    TitleBarColor = "#FF1A237E",
                    LaunchTarget  = "SysManagerWindow",
                    ControlClass  = "SysManagerPanelControl",
                    HasSaveButton = true,
                    SortOrder     = 1
                },
                new SysPanel
                {
                    PanelKey      = "DropZoneLauncher",
                    PanelName     = "📦 Drop Zone",
                    Description   = "Launcher — double-click to open Drop Zone file deployer",
                    IsVisible     = true,
                    IsPinned      = false,
                    PosLeft       = 210, PosTop = 0,
                    PanelWidth    = 80, PanelHeight = 80,
                    TitleBarColor = "#FF4A148C",
                    LaunchTarget  = "DropZoneWindow",
                    ControlClass  = "DropZoneLauncherControl",
                    HasSaveButton = false,
                    SortOrder     = 5
                },
                new SysPanel
                {
                    PanelKey      = "GitHubLauncher",
                    PanelName     = "🐙 GitHub",
                    Description   = "Launcher — double-click to open GitHub Push modal",
                    IsVisible     = true,
                    IsPinned      = false,
                    PosLeft       = 120, PosTop = 0,
                    PanelWidth    = 80, PanelHeight = 80,
                    TitleBarColor = "#FF1B5E20",
                    LaunchTarget  = "GitHubPushWindow",
                    ControlClass  = "GitHubLauncherControl",
                    HasSaveButton = false,
                    SortOrder     = 4
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
