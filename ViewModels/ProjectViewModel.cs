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
        private readonly IThingsToDoRepository  _todoRepo  = new SqliteThingsToDoRepository();
        private readonly IAppSettingsRepository _settings  = new SqliteAppSettingsRepository();

        public PanelManagerService PanelManager { get; } = new();

        // ── Project state ─────────────────────────────────────────────────
        [ObservableProperty] private string  _projectName     = "Untitled Project";
        [ObservableProperty] private string? _projectFilePath;
        [ObservableProperty] private bool    _isDirty         = false;

        // ── Editor state ──────────────────────────────────────────────────
        [ObservableProperty] private string  _activeXaml       = string.Empty;
        [ObservableProperty] private string  _activeCodeBehind = string.Empty;
        [ObservableProperty] private string? _activeFileName;

        // ── ThingsToDo ────────────────────────────────────────────────────
        [ObservableProperty] private ObservableCollection<ThingsToDo> _thingsToDo  = new();
        [ObservableProperty] private string  _newNoteText     = string.Empty;
        [ObservableProperty] private int     _newNotePhase    = 1;
        [ObservableProperty] private int     _newNotePriority = 2;

        // ── Status ────────────────────────────────────────────────────────
        [ObservableProperty] private string _statusMessage = "Ready";
        [ObservableProperty] private string _appVersion    = "MiniIDEv04";

        // ── Init ──────────────────────────────────────────────────────────

        public async Task InitializeAsync()
        {
            AppVersion = await _settings.GetValueAsync("AppVersion") ?? "MiniIDEv04";
            await PanelManager.LoadAsync();
            await LoadNotes();
            StatusMessage = $"{AppVersion} ready  —  {ThingsToDo.Count(t => !t.IsComplete)} open notes";
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

        // ── Placeholder project commands (Phase 4) ────────────────────────

        [RelayCommand] private void NewProject()  => StatusMessage = "New project — Phase 4";
        [RelayCommand] private void OpenProject() => StatusMessage = "Open project — Phase 4";
        [RelayCommand] private void SaveProject() => StatusMessage = "Save project — Phase 4";
        [RelayCommand] private void ExportSln()   => StatusMessage = "Export .sln — Phase 4";
        [RelayCommand] private void RunPreview()  => StatusMessage = "Preview — Phase 3";
    }
}
