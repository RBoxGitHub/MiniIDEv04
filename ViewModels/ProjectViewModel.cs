using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiniIDEv04.Data;
using MiniIDEv04.Data.Interfaces;
using MiniIDEv04.Data.Sqlite;
using MiniIDEv04.Models;
using MiniIDEv04.Services;
using System.Collections.ObjectModel;

namespace MiniIDEv04.ViewModels
{
    public partial class ProjectViewModel : ObservableObject
    {
        // ── Services ──────────────────────────────────────────────────────
        private readonly IThingsToDoRepository  _todoRepo = new SqliteThingsToDoRepository();
        private readonly IAppSettingsRepository _settings = new SqliteAppSettingsRepository();
        private readonly LibraryScanner         _scanner  = new();

        public PanelManagerService PanelManager    { get; } = new();
        public ToolboxRegistry     ToolboxRegistry { get; } = new();
        public DllWatcherService   DllWatcher      { get; }

        // ── Project state ─────────────────────────────────────────────────
        [ObservableProperty] private string  _projectName     = "Untitled Project";
        [ObservableProperty] private string? _projectFilePath;
        [ObservableProperty] private bool    _isDirty         = false;

        // ── Editor state ──────────────────────────────────────────────────
        [ObservableProperty] private string  _activeXaml       = string.Empty;
        [ObservableProperty] private string  _activeCodeBehind = string.Empty;
        [ObservableProperty] private string? _activeFileName;

        // ── ThingsToDo ────────────────────────────────────────────────────
        [ObservableProperty] private ObservableCollection<ThingsToDo> _thingsToDo = new();
        [ObservableProperty] private string _newNoteText     = string.Empty;
        [ObservableProperty] private int    _newNotePhase    = 1;
        [ObservableProperty] private int    _newNotePriority = 2;

        // ── Toolbox ───────────────────────────────────────────────────────
        public ObservableCollection<ToolboxGroupViewModel> ToolboxGroups
            => ToolboxRegistry.FilteredGroups;

        // ── Scan state ────────────────────────────────────────────────────
        [ObservableProperty] private bool   _isScanning   = false;
        [ObservableProperty] private string _scanStatus   = string.Empty;
        [ObservableProperty] private int    _scanProgress = 0;

        // ── DLL watcher state ─────────────────────────────────────────────
        [ObservableProperty] private bool   _isWatcherActive  = false;
        [ObservableProperty] private string _watcherStatus    = "DLL watcher not started";
        [ObservableProperty] private string _watchFolder      = string.Empty;

        // ── Status ────────────────────────────────────────────────────────
        [ObservableProperty] private string _statusMessage = "Ready";
        [ObservableProperty] private string _appVersion    = "MiniIDEv04";

        public ProjectViewModel()
        {
            DllWatcher = new DllWatcherService(ToolboxRegistry);
        }

        // ── Init ──────────────────────────────────────────────────────────
        public async Task InitializeAsync()
        {
            AppVersion = await _settings.GetValueAsync("AppVersion") ?? "MiniIDEv04";
            await PanelManager.LoadAsync();
            await LoadNotes();
            await ToolboxRegistry.LoadAsync();

            // Start DLL watcher
            await StartDllWatcherAsync();

            StatusMessage = $"{AppVersion} ready — {ThingsToDo.Count(t => !t.IsComplete)} open notes";
        }

        // ── DLL watcher ───────────────────────────────────────────────────

        private async Task StartDllWatcherAsync()
        {
            try
            {
                var folder = await _settings.GetValueAsync("ToolboxScanPath");

                // Resolve relative paths against AppContext.BaseDirectory
                if (string.IsNullOrWhiteSpace(folder))
                    folder = "ZipDrop";

                if (!System.IO.Path.IsPathRooted(folder))
                    folder = System.IO.Path.Combine(AppContext.BaseDirectory, folder);

                WatchFolder     = folder;
                IsWatcherActive = true;
                WatcherStatus   = $"👁 Watching: {System.IO.Path.GetFileName(folder)}";

                DllWatcher.Start(folder);

                // Wire progress so auto-watch scans flow to the UI scan bar
                DllWatcher.ExternalProgress = new Progress<LibraryScanProgress>(p =>
                {
                    ScanStatus  = p.Message;
                    IsScanning  = p.ProgressPercent < 100 && p.ProgressPercent >= 0;
                    if (p.ProgressPercent >= 0)
                        ScanProgress = p.ProgressPercent;
                });

                DllWatcher.DllScanned += OnDllScanned;
            }
            catch (Exception ex)
            {
                IsWatcherActive = false;
                WatcherStatus   = $"⚠ Watcher failed: {ex.Message}";
            }
        }

        private void OnDllScanned(object? sender, DllScannedEventArgs e)
        {
            IsWatcherActive = true;
            IsScanning      = false;
            ScanProgress    = 100;

            WatcherStatus = e.HasErrors
                ? $"⚠ {e.DllName} — {e.EntriesAdded} controls, {e.Errors.Count} errors"
                : $"✅ {e.DllName} — {e.EntriesAdded} new controls added";

            ScanStatus = e.HasErrors
                ? $"⚠ Done with {e.Errors.Count} error(s) — {e.EntriesAdded} controls added"
                : $"✅ Done — {e.EntriesAdded} new controls added from {e.DllName}";

            StatusMessage = e.HasErrors
                ? $"⚠ DLL scan: {e.LibraryName} — {e.EntriesAdded} controls, {e.Errors.Count} errors"
                : $"🔌 New library: {e.LibraryName} — {e.EntriesAdded} controls added to toolbox";
        }

        /// <summary>
        /// Manually trigger a DLL scan — called from ToolboxWindow "Scan DLL" button.
        /// </summary>
        /// <summary>
        /// Manually trigger a DLL scan — called directly from ToolboxWindow.
        /// </summary>
        public async Task ScanDllDirectAsync(string dllPath)
        {
            if (string.IsNullOrWhiteSpace(dllPath) || IsScanning) return;

            IsScanning    = true;
            ScanProgress  = 0;
            ScanStatus    = $"Scanning {System.IO.Path.GetFileName(dllPath)}...";
            StatusMessage = $"🔌 Scanning DLL: {System.IO.Path.GetFileName(dllPath)}";

            var progress = new Progress<LibraryScanProgress>(p =>
            {
                ScanStatus = p.Message;
                if (p.ProgressPercent >= 0)
                    ScanProgress = p.ProgressPercent;
            });

            try
            {
                await DllWatcher.ProcessDllAsync(dllPath, progress,
                    saveLog: _saveScanLog);
                ScanProgress = 100;
            }
            catch (Exception ex)
            {
                ScanStatus    = $"❌ {ex.Message}";
                StatusMessage = $"❌ DLL scan failed: {ex.Message}";
            }
            finally
            {
                IsScanning = false;
            }
        }

        /// <summary>Whether to save a .log file after each DLL scan.</summary>
        private bool _saveScanLog = false;
        public bool SaveScanLog
        {
            get => _saveScanLog;
            set => SetProperty(ref _saveScanLog, value);
        }

        // ── Notes commands ────────────────────────────────────────────────
        [RelayCommand]
        private async Task LoadNotes()
        {
            var notes = await _todoRepo.GetAllAsync();
            ThingsToDo.Clear();
            foreach (var n in notes) ThingsToDo.Add(n);
        }

        [RelayCommand]
        private async Task AddNote()
        {
            if (string.IsNullOrWhiteSpace(NewNoteText)) return;
            await _todoRepo.QuickAddAsync(NewNoteText,
                phase: NewNotePhase, priority: NewNotePriority);
            NewNoteText = string.Empty;
            await LoadNotes();
            StatusMessage = "Note saved.";
        }

        [RelayCommand]
        private async Task MarkComplete(ThingsToDo item)
        {
            await _todoRepo.MarkCompleteAsync(item.Id);
            await LoadNotes();
        }

        [RelayCommand]
        private async Task DeleteNote(ThingsToDo item)
        {
            await _todoRepo.DeleteAsync(item.Id);
            await LoadNotes();
        }

        // ── Panel commands ────────────────────────────────────────────────
        [RelayCommand]
        private Task TogglePanelAsync(string panelKey)
            => PanelManager.ToggleAsync(panelKey);

        [RelayCommand]
        private Task ClonePanelAsync(string panelKey)
            => PanelManager.CloneAsync(panelKey, $"Clone of {panelKey}");

        // ── Toolbox commands ──────────────────────────────────────────────
        [RelayCommand]
        private async Task LoadToolbox()
        {
            await ToolboxRegistry.LoadAsync();
            StatusMessage = $"Toolbox loaded — {ToolboxRegistry.TotalEntryCount} controls in {ToolboxRegistry.GroupCount} groups.";
        }

        public void FilterToolbox(string? term)
            => ToolboxRegistry.ApplyFilter(term);

        // ── Scanner command ───────────────────────────────────────────────
        [RelayCommand]
        private async Task ScanLibraries()
        {
            if (IsScanning) return;

            IsScanning    = true;
            ScanProgress  = 0;
            ScanStatus    = "Starting scan...";
            StatusMessage = "🔍 Scanning libraries...";

            var progress = new Progress<LibraryScanProgress>(p =>
            {
                ScanStatus = p.Message;
                if (p.ProgressPercent >= 0)
                    ScanProgress = p.ProgressPercent;
            });

            try
            {
                var result = await _scanner.ScanAllAsync(progress);

                if (result.HasErrors)
                {
                    ScanStatus    = $"Scan finished with {result.Errors.Count} error(s).";
                    StatusMessage = $"⚠ Scan completed — {result.EntriesAdded} controls added, {result.Errors.Count} errors.";
                }
                else
                {
                    ScanStatus    = $"✅ Done — {result.EntriesAdded} controls added across {result.LibrariesScanned} libraries.";
                    StatusMessage = $"🔍 Scan complete — {result.EntriesAdded} new controls added.";
                }

                ScanProgress = 100;
                await ToolboxRegistry.ReloadAsync();
                StatusMessage += $" · Toolbox refreshed ({ToolboxRegistry.TotalEntryCount} total controls)";
            }
            catch (Exception ex)
            {
                ScanStatus    = $"❌ Scan failed: {ex.Message}";
                StatusMessage = "❌ Library scan failed.";
            }
            finally
            {
                IsScanning = false;
            }
        }

        // ── Placeholder project commands (Phase 4) ────────────────────────
        [RelayCommand] private void NewProject()  => StatusMessage = "New project — Phase 4";
        [RelayCommand] private void OpenProject() => StatusMessage = "Open project — Phase 4";
        [RelayCommand] private void SaveProject() => StatusMessage = "Save project — Phase 4";
        [RelayCommand] private void ExportSln()   => StatusMessage = "Export .sln — Phase 4";
        [RelayCommand] private void RunPreview()  => StatusMessage = "Preview — Phase 3";
    }
}
