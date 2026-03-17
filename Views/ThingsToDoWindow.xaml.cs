using MiniIDEv04.Data;
using MiniIDEv04.Data.Sqlite;
using MiniIDEv04.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace MiniIDEv04.Views
{
    public partial class ThingsToDoWindow : Window
    {
        private readonly SqliteThingsToDoRepository _repo = new();
        private ObservableCollection<ThingsToDo> _allNotes = new();

        public ThingsToDoWindow()
        {
            InitializeComponent();
            Loaded += async (_, _) =>
            {
                DbPathTextBox.Text = ProjectDatabase.DbPath;
                await LoadNotesAsync();
                await ApplySaveButtonVisibilityAsync();
            };
        }

        private async Task ApplySaveButtonVisibilityAsync()
        {
            var repo  = new SqliteSysPanelRepository();
            var panel = await repo.GetByKeyAsync("SysManagerLauncher");
            SaveChangesButton.Visibility = (panel?.HasSaveButton == true)
                ? Visibility.Visible : Visibility.Collapsed;
        }

        // ── Data ──────────────────────────────────────────────────────────

        private async Task LoadNotesAsync()
        {
            var notes = await _repo.GetAllAsync();
            _allNotes = new ObservableCollection<ThingsToDo>(notes);
            ApplyFilters();
            StatusText.Text = $"{_allNotes.Count} total  ·  {_allNotes.Count(n => !n.IsComplete)} open";
        }

        private void ApplyFilters()
        {
            var filtered = _allNotes.AsEnumerable();

            if (FilterOpen.IsChecked == true)
                filtered = filtered.Where(n => !n.IsComplete);
            if (FilterHigh.IsChecked == true)
                filtered = filtered.Where(n => n.Priority == 1);
            if (FilterPh1.IsChecked == true)
                filtered = filtered.Where(n => n.Phase == 1);
            if (FilterPh2.IsChecked == true)
                filtered = filtered.Where(n => n.Phase == 2);
            if (FilterPh3.IsChecked == true)
                filtered = filtered.Where(n => n.Phase == 3);
            if (FilterPh4.IsChecked == true)
                filtered = filtered.Where(n => n.Phase == 4);

            NotesGrid.ItemsSource = new ObservableCollection<ThingsToDo>(filtered);
            RecordCountText.Text  = $"{((ObservableCollection<ThingsToDo>)NotesGrid.ItemsSource).Count} shown";
        }

        // ── Event handlers ─────────────────────────────────────────────────

        private async void AddNote_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NoteTextBox.Text)) return;

            var phase    = PhaseCombo.SelectedIndex    + 1;
            var priority = PriorityCombo.SelectedIndex + 1;

            await _repo.QuickAddAsync(NoteTextBox.Text, phase: phase, priority: priority);
            NoteTextBox.Text = string.Empty;
            await LoadNotesAsync();
            StatusText.Text = "Note saved.";
        }

        private void Filter_Changed(object sender, RoutedEventArgs e)
            => ApplyFilters();

        private async void NotesGrid_RowEditEnded(object sender, Telerik.Windows.Controls.GridViewRowEditEndedEventArgs e)
        {
            if (e.Row?.DataContext is not ThingsToDo note) return;

            note.UpdatedAt = DateTime.UtcNow;
            await _repo.UpdateAsync(note);

            // Refresh count in footer
            StatusText.Text = $"{_allNotes.Count} total  ·  {_allNotes.Count(n => !n.IsComplete)} open";
        }

        private async void BrowseDbPath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title            = "Select miniIDE database",
                Filter           = "SQLite Database (*.db)|*.db|All files (*.*)|*.*",
                InitialDirectory = System.IO.Path.GetDirectoryName(ProjectDatabase.DbPath)
                                   ?? AppContext.BaseDirectory,
                FileName         = "miniIDE.db"
            };

            if (dialog.ShowDialog() != true) return;

            ProjectDatabase.SetDbPath(dialog.FileName);
            DbPathTextBox.Text = dialog.FileName;
            await LoadNotesAsync();
            StatusText.Text = $"DB switched → {System.IO.Path.GetFileName(dialog.FileName)}";
        }

        private async void ResetDbPath_Click(object sender, RoutedEventArgs e)
        {
            ProjectDatabase.ResetDbPath();
            DbPathTextBox.Text = ProjectDatabase.DbPath;
            await LoadNotesAsync();
            StatusText.Text = "DB reset to default.";
        }

        private async void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            var notes = NotesGrid.ItemsSource as IEnumerable<ThingsToDo>;
            if (notes is null) return;

            int count = 0;
            foreach (var note in notes)
            {
                note.UpdatedAt = DateTime.UtcNow;
                await _repo.UpdateAsync(note);
                count++;
            }

            StatusText.Text = $"✓  {count} rows saved  ·  {DateTime.Now:HH:mm:ss}";
        }

        private async void DeleteNote_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            if (btn.Tag is not int id) return;

            var result = MessageBox.Show(
                "Delete this note?", "Confirm Delete",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            await _repo.DeleteAsync(id);
            await LoadNotesAsync();
            StatusText.Text = "Note deleted.";
        }

        private void Close_Click(object sender, RoutedEventArgs e)
            => Close();
    }
}
