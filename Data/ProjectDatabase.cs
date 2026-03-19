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

            // ── Phase 1 tables ────────────────────────────────────────────
            db.CreateTable<ThingsToDo>();
            db.CreateTable<SysPanel>();
            db.CreateTable<SysAppSetting>();
            db.CreateTable<SysControl>();
            db.CreateTable<SysControlProperty>();
            db.CreateTable<SysGitLog>();
            db.CreateTable<SysDropLog>();

            // ── Phase 2 tables ────────────────────────────────────────────
            db.CreateTable<SysLibrary>();
            db.CreateTable<SysToolboxGroup>();
            db.CreateTable<SysToolboxEntry>();

            // ── Migrations — safe to run on existing DB ───────────────────
            MigrateAddColumnIfMissing(db, "sys_Panels", "ControlClass",    "TEXT DEFAULT ''");
            MigrateAddColumnIfMissing(db, "sys_Panels", "HasSaveButton",   "INTEGER DEFAULT 0");
            MigrateAddColumnIfMissing(db, "sys_Panels", "LaunchTarget",    "TEXT DEFAULT ''");

            SeedAppSettings(db);
            SeedPanels(db);
            SeedThingsToDo(db);
            SeedLibraries(db);
            SeedToolboxGroups(db);
            SeedToolboxEntries(db);

            // ── Back-fill ControlClass for existing rows ──────────────────
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
                ["QuickAddPanel"]       = "QuickAddPanelControl",
                ["SysManagerLauncher"]  = "SysManagerLauncherControl",
                ["SysManagerPanel"]     = "SysManagerPanelControl",
                ["GitHubLauncher"]      = "GitHubLauncherControl",
                ["DropZoneLauncher"]    = "DropZoneLauncherControl",
                ["ToolboxLauncher"]     = "ToolboxLauncherControl",
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

            // Insert GitHubLauncher if missing
            var existingGh = db.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM sys_Panels WHERE PanelKey='GitHubLauncher'");
            if (existingGh == 0)
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

            // Insert ToolboxLauncher if missing
            var existingTb = db.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM sys_Panels WHERE PanelKey='ToolboxLauncher'");
            if (existingTb == 0)
                db.Execute(@"
                    INSERT INTO sys_Panels
                        (PanelKey, PanelName, Description, IsVisible, IsPinned, IsCloned,
                         PosLeft, PosTop, PanelWidth, PanelHeight, TitleBarColor,
                         LaunchTarget, ControlClass, HasSaveButton, Version, SortOrder,
                         CreatedAt, UpdatedAt)
                    VALUES
                        ('ToolboxLauncher','🧰 Toolbox','Launcher — double-click to open Toolbox',
                         1,0,0, 300,0,80,80,'#FF006064',
                         'ToolboxWindow','ToolboxLauncherControl',0,'4.0.0',6,
                         datetime('now'),datetime('now'))");

            // Remove old GitHubPushPanel if present
            db.Execute("DELETE FROM sys_Panels WHERE PanelKey='GitHubPushPanel'");
        }

        // ── Seeds ──────────────────────────────────────────────────────────

        private static void SeedAppSettings(SQLiteConnection db)
        {
            if (db.Table<SysAppSetting>().Count() > 0) return;

            db.InsertAll(new[]
            {
                new SysAppSetting { Key = "AppVersion",          Value = "MiniIDEv04",    Description = "Current IDE version identifier" },
                new SysAppSetting { Key = "DefaultTheme",        Value = "Windows11",     Description = "Telerik theme applied at startup" },
                new SysAppSetting { Key = "ProjectNotesPath",    Value = "ProjectNotes",  Description = "Relative path to the local SQLite DB folder" },
                new SysAppSetting { Key = "SqlServerConnection", Value = "",              Description = "SQL Server connection string — Phase 4 exported .sln projects only" },
                new SysAppSetting { Key = "ProjectRootPath",     Value = @"D:\GrokCryptoTrack\Production-Claude\MiniIDE-WorkFolder\MiniIDEv04", Description = "Root folder of the MiniIDEv04 project source" },
                new SysAppSetting { Key = "ZipWorkFolder",       Value = "ZipDrop",       Description = "Relative path to zip drop folder — resolved from AppContext.BaseDirectory" },
                new SysAppSetting { Key = "ToolboxScanPath",     Value = "ZipDrop",       Description = "Folder watched for DLL drop-ins — Phase 2 Item 7" },
            });
        }

        private static void SeedPanels(SQLiteConnection db)
        {
            if (db.Table<SysPanel>().Count() > 0) return;

            db.InsertAll(new[]
            {
                new SysPanel { PanelKey = "QuickAddPanel",      PanelName = "📝 Quick Add Note",  Description = "Floating quick-add bar",                          IsVisible = true,  PosLeft = 20,  PosTop = 0, PanelWidth = 620, PanelHeight = 56,  TitleBarColor = "#FF37474F", LaunchTarget = "",               ControlClass = "QuickAddPanelControl",       HasSaveButton = false, SortOrder = 2 },
                new SysPanel { PanelKey = "SysManagerLauncher", PanelName = "📝 Notes Launcher",  Description = "Double-click to open ThingsToDo modal",           IsVisible = true,  PosLeft = 20,  PosTop = 0, PanelWidth = 80,  PanelHeight = 80,  TitleBarColor = "#FF37474F", LaunchTarget = "ThingsToDoWindow",   ControlClass = "SysManagerLauncherControl",  HasSaveButton = true,  SortOrder = 0 },
                new SysPanel { PanelKey = "SysManagerPanel",    PanelName = "⚙ Sys Manager",      Description = "Double-click to open SysManager modal",           IsVisible = true,  PosLeft = 20,  PosTop = 700, PanelWidth = 120, PanelHeight = 80, TitleBarColor = "#FF1A237E", LaunchTarget = "SysManagerWindow",   ControlClass = "SysManagerPanelControl",     HasSaveButton = true,  SortOrder = 1, IsPinned = true },
                new SysPanel { PanelKey = "GitHubLauncher",     PanelName = "🐙 GitHub",          Description = "Double-click to open GitHub Push modal",          IsVisible = true,  PosLeft = 120, PosTop = 0, PanelWidth = 80,  PanelHeight = 80,  TitleBarColor = "#FF1B5E20", LaunchTarget = "GitHubPushWindow",   ControlClass = "GitHubLauncherControl",      HasSaveButton = false, SortOrder = 4 },
                new SysPanel { PanelKey = "DropZoneLauncher",   PanelName = "📦 Drop Zone",       Description = "Double-click to open Drop Zone file deployer",    IsVisible = true,  PosLeft = 210, PosTop = 0, PanelWidth = 80,  PanelHeight = 80,  TitleBarColor = "#FF4A148C", LaunchTarget = "DropZoneWindow",      ControlClass = "DropZoneLauncherControl",    HasSaveButton = false, SortOrder = 5 },
                new SysPanel { PanelKey = "ToolboxLauncher",    PanelName = "🧰 Toolbox",         Description = "Double-click to open Toolbox window",             IsVisible = true,  PosLeft = 300, PosTop = 0, PanelWidth = 80,  PanelHeight = 80,  TitleBarColor = "#FF006064", LaunchTarget = "ToolboxWindow",       ControlClass = "ToolboxLauncherControl",     HasSaveButton = false, SortOrder = 6 },
            });
        }

        private static void SeedLibraries(SQLiteConnection db)
        {
            if (db.Table<SysLibrary>().Count() > 0) return;

            db.InsertAll(new[]
            {
                new SysLibrary
                {
                    LibraryKey   = "WPF_Builtin",
                    LibraryName  = "WPF Built-in Controls",
                    AssemblyName = "PresentationFramework",
                    AssemblyPath = "",
                    Version      = "4.0",
                    Tier         = 0,
                    IsScanEnabled = false,   // seeded manually — scanner not needed
                    IsLoaded     = true,
                    IsVisible    = true,
                },
                new SysLibrary
                {
                    LibraryKey   = "Telerik_WPF",
                    LibraryName  = "Telerik UI for WPF",
                    AssemblyName = "Telerik.Windows.Controls",
                    AssemblyPath = "",
                    Version      = "2024.1",
                    Tier         = 1,
                    IsScanEnabled = true,    // Item 4: LibraryScanner will scan this
                    IsLoaded     = true,
                    IsVisible    = true,
                },
            });
        }

        private static void SeedToolboxGroups(SQLiteConnection db)
        {
            if (db.Table<SysToolboxGroup>().Count() > 0) return;

            db.InsertAll(new[]
            {
                new SysToolboxGroup { GroupKey = "WPF_Layout",    GroupName = "Layout",     Icon = "📐", SortOrder = 0, Tier = 0 },
                new SysToolboxGroup { GroupKey = "WPF_Input",     GroupName = "Input",      Icon = "⌨",  SortOrder = 1, Tier = 0 },
                new SysToolboxGroup { GroupKey = "WPF_Display",   GroupName = "Display",    Icon = "🖼",  SortOrder = 2, Tier = 0 },
                new SysToolboxGroup { GroupKey = "WPF_Data",      GroupName = "Data",       Icon = "📊", SortOrder = 3, Tier = 0 },
                new SysToolboxGroup { GroupKey = "WPF_Navigation",GroupName = "Navigation", Icon = "🧭", SortOrder = 4, Tier = 0 },
                new SysToolboxGroup { GroupKey = "Telerik",       GroupName = "Telerik",    Icon = "⚡", SortOrder = 5, Tier = 1 },
            });
        }

        private static void SeedToolboxEntries(SQLiteConnection db)
        {
            if (db.Table<SysToolboxEntry>().Count() > 0) return;

            const string wpfXmlns  = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
            const string wpfAssembly = "PresentationFramework";

            db.InsertAll(new[]
            {
                // ── Layout ───────────────────────────────────────────────
                new SysToolboxEntry { GroupKey = "WPF_Layout", DisplayName = "Grid",         TypeFullName = "System.Windows.Controls.Grid",          AssemblyName = wpfAssembly, XmlNamespace = wpfXmlns, DefaultXamlSnippet = "<Grid Width=\"200\" Height=\"200\"/>",                          Icon = "▦", SortOrder = 0 },
                new SysToolboxEntry { GroupKey = "WPF_Layout", DisplayName = "StackPanel",   TypeFullName = "System.Windows.Controls.StackPanel",    AssemblyName = wpfAssembly, XmlNamespace = wpfXmlns, DefaultXamlSnippet = "<StackPanel Width=\"200\" Height=\"200\"/>",                    Icon = "☰", SortOrder = 1 },
                new SysToolboxEntry { GroupKey = "WPF_Layout", DisplayName = "DockPanel",    TypeFullName = "System.Windows.Controls.DockPanel",     AssemblyName = wpfAssembly, XmlNamespace = wpfXmlns, DefaultXamlSnippet = "<DockPanel Width=\"200\" Height=\"200\"/>",                     Icon = "⊞", SortOrder = 2 },
                new SysToolboxEntry { GroupKey = "WPF_Layout", DisplayName = "WrapPanel",    TypeFullName = "System.Windows.Controls.WrapPanel",     AssemblyName = wpfAssembly, XmlNamespace = wpfXmlns, DefaultXamlSnippet = "<WrapPanel Width=\"200\" Height=\"200\"/>",                     Icon = "↵", SortOrder = 3 },
                new SysToolboxEntry { GroupKey = "WPF_Layout", DisplayName = "Canvas",       TypeFullName = "System.Windows.Controls.Canvas",        AssemblyName = wpfAssembly, XmlNamespace = wpfXmlns, DefaultXamlSnippet = "<Canvas Width=\"200\" Height=\"200\"/>",                       Icon = "⬜", SortOrder = 4 },
                new SysToolboxEntry { GroupKey = "WPF_Layout", DisplayName = "Border",       TypeFullName = "System.Windows.Controls.Border",        AssemblyName = wpfAssembly, XmlNamespace = wpfXmlns, DefaultXamlSnippet = "<Border Width=\"200\" Height=\"100\" BorderThickness=\"1\"/>", Icon = "⬚", SortOrder = 5 },
                new SysToolboxEntry { GroupKey = "WPF_Layout", DisplayName = "ScrollViewer", TypeFullName = "System.Windows.Controls.ScrollViewer",  AssemblyName = wpfAssembly, XmlNamespace = wpfXmlns, DefaultXamlSnippet = "<ScrollViewer Width=\"200\" Height=\"200\"/>",                  Icon = "↕", SortOrder = 6 },

                // ── Input ────────────────────────────────────────────────
                new SysToolboxEntry { GroupKey = "WPF_Input", DisplayName = "Button",       TypeFullName = "System.Windows.Controls.Button",        AssemblyName = wpfAssembly, XmlNamespace = wpfXmlns, DefaultXamlSnippet = "<Button Content=\"Button\" Width=\"80\" Height=\"30\"/>",       Icon = "🔘", SortOrder = 0 },
                new SysToolboxEntry { GroupKey = "WPF_Input", DisplayName = "TextBox",      TypeFullName = "System.Windows.Controls.TextBox",       AssemblyName = wpfAssembly, XmlNamespace = wpfXmlns, DefaultXamlSnippet = "<TextBox Width=\"200\" Height=\"30\"/>",                        Icon = "✏", SortOrder = 1 },
                new SysToolboxEntry { GroupKey = "WPF_Input", DisplayName = "CheckBox",     TypeFullName = "System.Windows.Controls.CheckBox",      AssemblyName = wpfAssembly, XmlNamespace = wpfXmlns, DefaultXamlSnippet = "<CheckBox Content=\"CheckBox\"/>",                             Icon = "☑", SortOrder = 2 },
                new SysToolboxEntry { GroupKey = "WPF_Input", DisplayName = "RadioButton",  TypeFullName = "System.Windows.Controls.RadioButton",   AssemblyName = wpfAssembly, XmlNamespace = wpfXmlns, DefaultXamlSnippet = "<RadioButton Content=\"Option\"/>",                            Icon = "⊙", SortOrder = 3 },
                new SysToolboxEntry { GroupKey = "WPF_Input", DisplayName = "ComboBox",     TypeFullName = "System.Windows.Controls.ComboBox",      AssemblyName = wpfAssembly, XmlNamespace = wpfXmlns, DefaultXamlSnippet = "<ComboBox Width=\"150\" Height=\"30\"/>",                      Icon = "⏷", SortOrder = 4 },
                new SysToolboxEntry { GroupKey = "WPF_Input", DisplayName = "Slider",       TypeFullName = "System.Windows.Controls.Slider",        AssemblyName = wpfAssembly, XmlNamespace = wpfXmlns, DefaultXamlSnippet = "<Slider Width=\"200\" Minimum=\"0\" Maximum=\"100\"/>",        Icon = "⊢", SortOrder = 5 },
                new SysToolboxEntry { GroupKey = "WPF_Input", DisplayName = "PasswordBox",  TypeFullName = "System.Windows.Controls.PasswordBox",   AssemblyName = wpfAssembly, XmlNamespace = wpfXmlns, DefaultXamlSnippet = "<PasswordBox Width=\"200\" Height=\"30\"/>",                   Icon = "🔒", SortOrder = 6 },

                // ── Display ──────────────────────────────────────────────
                new SysToolboxEntry { GroupKey = "WPF_Display", DisplayName = "TextBlock",  TypeFullName = "System.Windows.Controls.TextBlock",     AssemblyName = wpfAssembly, XmlNamespace = wpfXmlns, DefaultXamlSnippet = "<TextBlock Text=\"TextBlock\"/>",                              Icon = "T",  SortOrder = 0 },
                new SysToolboxEntry { GroupKey = "WPF_Display", DisplayName = "Label",      TypeFullName = "System.Windows.Controls.Label",         AssemblyName = wpfAssembly, XmlNamespace = wpfXmlns, DefaultXamlSnippet = "<Label Content=\"Label\"/>",                                  Icon = "🏷", SortOrder = 1 },
                new SysToolboxEntry { GroupKey = "WPF_Display", DisplayName = "Image",      TypeFullName = "System.Windows.Controls.Image",         AssemblyName = wpfAssembly, XmlNamespace = wpfXmlns, DefaultXamlSnippet = "<Image Width=\"100\" Height=\"100\" Stretch=\"Uniform\"/>",   Icon = "🖼", SortOrder = 2 },
                new SysToolboxEntry { GroupKey = "WPF_Display", DisplayName = "ProgressBar",TypeFullName = "System.Windows.Controls.ProgressBar",   AssemblyName = wpfAssembly, XmlNamespace = wpfXmlns, DefaultXamlSnippet = "<ProgressBar Width=\"200\" Height=\"20\" Value=\"50\"/>",     Icon = "▬", SortOrder = 3 },
                new SysToolboxEntry { GroupKey = "WPF_Display", DisplayName = "Separator",  TypeFullName = "System.Windows.Controls.Separator",     AssemblyName = wpfAssembly, XmlNamespace = wpfXmlns, DefaultXamlSnippet = "<Separator Width=\"200\"/>",                                  Icon = "─", SortOrder = 4 },

                // ── Data ─────────────────────────────────────────────────
                new SysToolboxEntry { GroupKey = "WPF_Data", DisplayName = "DataGrid",      TypeFullName = "System.Windows.Controls.DataGrid",      AssemblyName = wpfAssembly, XmlNamespace = wpfXmlns, DefaultXamlSnippet = "<DataGrid Width=\"400\" Height=\"200\" AutoGenerateColumns=\"True\"/>", Icon = "📋", SortOrder = 0 },
                new SysToolboxEntry { GroupKey = "WPF_Data", DisplayName = "ListBox",       TypeFullName = "System.Windows.Controls.ListBox",       AssemblyName = wpfAssembly, XmlNamespace = wpfXmlns, DefaultXamlSnippet = "<ListBox Width=\"200\" Height=\"200\"/>",                     Icon = "📄", SortOrder = 1 },
                new SysToolboxEntry { GroupKey = "WPF_Data", DisplayName = "ListView",      TypeFullName = "System.Windows.Controls.ListView",      AssemblyName = wpfAssembly, XmlNamespace = wpfXmlns, DefaultXamlSnippet = "<ListView Width=\"300\" Height=\"200\"/>",                    Icon = "📃", SortOrder = 2 },
                new SysToolboxEntry { GroupKey = "WPF_Data", DisplayName = "TreeView",      TypeFullName = "System.Windows.Controls.TreeView",      AssemblyName = wpfAssembly, XmlNamespace = wpfXmlns, DefaultXamlSnippet = "<TreeView Width=\"200\" Height=\"300\"/>",                    Icon = "🌲", SortOrder = 3 },

                // ── Navigation ───────────────────────────────────────────
                new SysToolboxEntry { GroupKey = "WPF_Navigation", DisplayName = "TabControl",   TypeFullName = "System.Windows.Controls.TabControl",   AssemblyName = wpfAssembly, XmlNamespace = wpfXmlns, DefaultXamlSnippet = "<TabControl Width=\"300\" Height=\"200\"/>",             Icon = "🗂", SortOrder = 0 },
                new SysToolboxEntry { GroupKey = "WPF_Navigation", DisplayName = "Expander",     TypeFullName = "System.Windows.Controls.Expander",     AssemblyName = wpfAssembly, XmlNamespace = wpfXmlns, DefaultXamlSnippet = "<Expander Header=\"Expander\" Width=\"200\"/>",          Icon = "▶", SortOrder = 1 },
                new SysToolboxEntry { GroupKey = "WPF_Navigation", DisplayName = "Menu",         TypeFullName = "System.Windows.Controls.Menu",         AssemblyName = wpfAssembly, XmlNamespace = wpfXmlns, DefaultXamlSnippet = "<Menu Width=\"300\"/>",                                 Icon = "☰", SortOrder = 2 },
                new SysToolboxEntry { GroupKey = "WPF_Navigation", DisplayName = "ContextMenu",  TypeFullName = "System.Windows.Controls.ContextMenu",  AssemblyName = wpfAssembly, XmlNamespace = wpfXmlns, DefaultXamlSnippet = "<ContextMenu/>",                                        Icon = "📋", SortOrder = 3 },

                // ── Telerik (placeholders — Item 4 LibraryScanner will populate fully) ──
                new SysToolboxEntry { GroupKey = "Telerik", DisplayName = "RadButton",      TypeFullName = "Telerik.Windows.Controls.RadButton",      AssemblyName = "Telerik.Windows.Controls", XmlNamespace = "http://schemas.telerik.com/2008/xaml/presentation", DefaultXamlSnippet = "<telerik:RadButton Content=\"RadButton\"/>",        Icon = "⚡", SortOrder = 0, Tier = 1 },
                new SysToolboxEntry { GroupKey = "Telerik", DisplayName = "RadComboBox",    TypeFullName = "Telerik.Windows.Controls.RadComboBox",    AssemblyName = "Telerik.Windows.Controls", XmlNamespace = "http://schemas.telerik.com/2008/xaml/presentation", DefaultXamlSnippet = "<telerik:RadComboBox Width=\"150\"/>",              Icon = "⚡", SortOrder = 1, Tier = 1 },
                new SysToolboxEntry { GroupKey = "Telerik", DisplayName = "RadGridView",    TypeFullName = "Telerik.Windows.Controls.RadGridView",    AssemblyName = "Telerik.Windows.Controls", XmlNamespace = "http://schemas.telerik.com/2008/xaml/presentation", DefaultXamlSnippet = "<telerik:RadGridView Width=\"400\" Height=\"200\"/>", Icon = "⚡", SortOrder = 2, Tier = 1 },
                new SysToolboxEntry { GroupKey = "Telerik", DisplayName = "RadTreeView",    TypeFullName = "Telerik.Windows.Controls.RadTreeView",    AssemblyName = "Telerik.Windows.Controls", XmlNamespace = "http://schemas.telerik.com/2008/xaml/presentation", DefaultXamlSnippet = "<telerik:RadTreeView Width=\"200\" Height=\"300\"/>", Icon = "⚡", SortOrder = 3, Tier = 1 },
                new SysToolboxEntry { GroupKey = "Telerik", DisplayName = "RadTabControl",  TypeFullName = "Telerik.Windows.Controls.RadTabControl",  AssemblyName = "Telerik.Windows.Controls", XmlNamespace = "http://schemas.telerik.com/2008/xaml/presentation", DefaultXamlSnippet = "<telerik:RadTabControl Width=\"300\" Height=\"200\"/>", Icon = "⚡", SortOrder = 4, Tier = 1 },
            });
        }

        private static void SeedThingsToDo(SQLiteConnection db)
        {
            if (db.Table<ThingsToDo>().Count() > 0) return;

            db.InsertAll(new[]
            {
                new ThingsToDo { SideNote = "MiniIDEv04 initialized. sys_ThingsToDo, sys_Panels, sys_AppSettings created. DraggablePanel wired. Repository interfaces in place.", ReferenceObject = "ProjectDatabase", TypeReference = "MiniIDEv04.Data.ProjectDatabase", FileName = "ProjectDatabase.cs", FilePath = "Data/ProjectDatabase.cs", Phase = 1, Priority = 3, IsComplete = true },
                new ThingsToDo { SideNote = "Phase 2: Build sys_ToolboxGroups, sys_ToolboxEntries, sys_Libraries. LibraryScanner (Tier 1 reflection), ToolboxRegistry, RadPanelBar wired, DLL drop-in folder watcher (Tier 2).", ReferenceObject = "LibraryScanner", TypeReference = "MiniIDEv04.Services.LibraryScanner", FileName = "LibraryScanner.cs", FilePath = "Services/LibraryScanner.cs", Phase = 2, Priority = 1 },
                new ThingsToDo { SideNote = "Phase 3: XamlRenderer (XamlReader.Load two-pass), RoslynCompiler (CSharpCompilation + dynamic assembly refs), XmlnsInjector, Tier 3 live control registration.", ReferenceObject = "RoslynCompiler", TypeReference = "MiniIDEv04.Services.RoslynCompiler", FileName = "RoslynCompiler.cs", FilePath = "Services/RoslynCompiler.cs", Phase = 3, Priority = 1 },
                new ThingsToDo { SideNote = "Phase 4: ProjectSerializer (.wpfproj JSON zip), SlnGenerator (.sln + .csproj templates), Dapper/SQL Server layer for exported project DBs only.", ReferenceObject = "SlnGenerator", TypeReference = "MiniIDEv04.Services.SlnGenerator", FileName = "SlnGenerator.cs", FilePath = "Services/SlnGenerator.cs", Phase = 4, Priority = 1 },
                new ThingsToDo { SideNote = "DraggablePanel — clean from scratch in MiniIDEv04.Controls. Title bar drag, resize grip, ShowCloseButton, ShowResizeGrip DPs. PositionChanged fires on mouse release only.", ReferenceObject = "DraggablePanel", TypeReference = "MiniIDEv04.Controls.DraggablePanel", FileName = "DraggablePanel.xaml", FilePath = "Controls/DraggablePanel.xaml", Phase = 1, Priority = 1 },
                new ThingsToDo { SideNote = "sys_Panels drives ALL DraggablePanel instances. PanelManagerService watches rows — IsVisible, PosLeft, PosTop, Width, Height, TitleBarColor all reflect live on screen.", ReferenceObject = "PanelManagerService", TypeReference = "MiniIDEv04.Services.PanelManagerService", FileName = "PanelManagerService.cs", FilePath = "Services/PanelManagerService.cs", Phase = 1, Priority = 1 },
                new ThingsToDo { SideNote = "Phase 2 Item 1 COMPLETE — Thumb.DragCompleted saves panel size to sys_Panels. NaN guard added for Canvas.GetLeft/GetTop.", ReferenceObject = "DraggablePanel", TypeReference = "MiniIDEv04.Controls.DraggablePanel", FileName = "DraggablePanel.xaml.cs", FilePath = "Controls/DraggablePanel.xaml.cs", Phase = 2, Priority = 2, IsComplete = true },
                new ThingsToDo { SideNote = "Phase 2 Item 2 COMPLETE — Dynamic Canvas spawn for cloned panels. CollectionChanged subscription in MainWindow wires SpawnSinglePanel on Add. CloneAsync bug fix: ControlClass + LaunchTarget now copied.", ReferenceObject = "PanelManagerService", TypeReference = "MiniIDEv04.Services.PanelManagerService", FileName = "PanelManagerService.cs", FilePath = "Services/PanelManagerService.cs", Phase = 2, Priority = 2, IsComplete = true },
                new ThingsToDo { SideNote = "Phase 2 Item 3 COMPLETE — sys_ToolboxGroups, sys_ToolboxEntries, sys_Libraries tables + seeds. ToolboxLauncherControl registered in sys_Panels and PanelControlFactory.", ReferenceObject = "ProjectDatabase", TypeReference = "MiniIDEv04.Data.ProjectDatabase", FileName = "ProjectDatabase.cs", FilePath = "Data/ProjectDatabase.cs", Phase = 2, Priority = 1, IsComplete = true },
                new ThingsToDo { SideNote = "Phase 2 Item 4 NEXT — LibraryScanner Tier 1 reflection scanner. Scans PresentationFramework + Telerik assemblies and populates sys_ToolboxEntries.", ReferenceObject = "LibraryScanner", TypeReference = "MiniIDEv04.Services.LibraryScanner", FileName = "LibraryScanner.cs", FilePath = "Services/LibraryScanner.cs", Phase = 2, Priority = 1 },
            });
        }

        // ── Connection factories ────────────────────────────────────────────

        public static SQLiteConnection GetConnection() => new(DbPath);
        public static SQLiteAsyncConnection GetAsyncConnection() => new(DbPath);

        public static void SetDbPath(string path)  => _dbPath = path;
        public static void ResetDbPath()            => _dbPath = null;
    }
}
