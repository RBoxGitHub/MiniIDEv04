using MiniIDEv04.Data.Sqlite;
using MiniIDEv04.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace MiniIDEv04.Views
{
    public partial class DropZoneWindow : Window
    {
        private readonly ZipDropService              _dropService = new();
        private readonly SqliteDropLogRepository     _dropLog     = new();
        private readonly SqliteAppSettingsRepository _settings    = new();

        private string _zipWorkFolder = string.Empty;

        public DropZoneWindow()
        {
            InitializeComponent();
            Loaded += async (_, _) => await InitAsync();
        }

        private async Task InitAsync()
        {
            // Load settings
            var root = await _settings.GetValueAsync("ProjectRootPath");
            ProjectRootBox.Text = string.IsNullOrWhiteSpace(root)
                ? @"D:\GrokCryptoTrack\Production-Claude\MiniIDE-WorkFolder\MiniIDEv04"
                : root;

            var folder = await _settings.GetValueAsync("ZipWorkFolder");
            _zipWorkFolder = string.IsNullOrWhiteSpace(folder)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
                : folder;

            RefreshZipFolder();
            await LoadRecentLogAsync();
        }

        // ── Left panel: zip folder browser ───────────────────────────────

        private void RefreshZipFolder()
        {
            ZipFolderBox.Text = _zipWorkFolder;

            if (!Directory.Exists(_zipWorkFolder))
            {
                ZipFileList.ItemsSource = null;
                return;
            }

            var zips = Directory.GetFiles(_zipWorkFolder, "*.zip")
                                .OrderByDescending(File.GetLastWriteTime)
                                .Select(Path.GetFileName)
                                .ToList();

            ZipFileList.ItemsSource = new ObservableCollection<string>(zips!);
        }

        private void ZipFileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ZipFileList.SelectedItem is string fileName)
                ZipPathBox.Text = Path.Combine(_zipWorkFolder, fileName);
        }

        private void RefreshZipList_Click(object sender, RoutedEventArgs e)
            => RefreshZipFolder();

        private async void BrowseZipFolder_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFolderDialog
            {
                Title = "Select Zip Work Folder"
            };
            if (dlg.ShowDialog() == true)
            {
                _zipWorkFolder = dlg.FolderName;
                await _settings.SetValueAsync("ZipWorkFolder", _zipWorkFolder);
                RefreshZipFolder();
            }
        }

        // ── Right panel: zip file + project root ──────────────────────────

        private void BrowseZip_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title            = "Select Claude Zip File",
                Filter           = "Zip Files (*.zip)|*.zip",
                InitialDirectory = Directory.Exists(_zipWorkFolder)
                    ? _zipWorkFolder
                    : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            };
            if (dlg.ShowDialog() == true)
                ZipPathBox.Text = dlg.FileName;
        }

        private async void BrowseRoot_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFolderDialog { Title = "Select Project Root Folder" };
            if (dlg.ShowDialog() == true)
            {
                ProjectRootBox.Text = dlg.FolderName;
                await _settings.SetValueAsync("ProjectRootPath", dlg.FolderName);
            }
        }

        // ── Process zip ───────────────────────────────────────────────────

        private async void ProcessZip_Click(object sender, RoutedEventArgs e)
        {
            var zipPath     = ZipPathBox.Text.Trim();
            var projectRoot = ProjectRootBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(zipPath) || !File.Exists(zipPath))
            { StatusText.Text = "⚠  Select a zip file first."; return; }

            if (string.IsNullOrWhiteSpace(projectRoot) || !Directory.Exists(projectRoot))
            { StatusText.Text = "⚠  Project root folder not found."; return; }

            StatusText.Text = "Processing...";

            try
            {
                var results = await Task.Run(() =>
                    _dropService.ProcessZip(zipPath, projectRoot));

                ResultsGrid.ItemsSource = new ObservableCollection<ZipDropService.DropResult>(results);

                FileCountText.Text =
                    $"{results.Count} files  ·  " +
                    $"{results.Count(r => r.Status == "New")} new  ·  " +
                    $"{results.Count(r => r.Status == "Updated")} updated";

                foreach (var r in results)
                    await _dropLog.LogDropAsync(
                        Path.GetFileName(zipPath),
                        r.FileName, r.Destination, r.Status);

                StatusText.Text =
                    $"✅  Done — {results.Count} files deployed  ·  {DateTime.Now:HH:mm:ss}";

                // Refresh zip list in case new zips appeared
                RefreshZipFolder();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"❌  {ex.Message}";
            }
        }

        // ── Recent log ────────────────────────────────────────────────────

        private async Task LoadRecentLogAsync()
        {
            var logs = await _dropLog.GetRecentAsync(100);
            if (logs.Count == 0) return;

            var display = logs.Select(l => new ZipDropService.DropResult(
                l.ZipFileName, l.FileName, l.Destination, "", l.Status)).ToList();

            ResultsGrid.ItemsSource = new ObservableCollection<ZipDropService.DropResult>(display);
            FileCountText.Text      = $"{logs.Count} recent drop records";
            StatusText.Text         = "Showing recent history — select a zip to deploy";
        }

        // ── Clear log ─────────────────────────────────────────────────────

        private async void ClearLog_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Clear all drop log entries?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            var logs = await _dropLog.GetAllAsync();
            foreach (var log in logs)
                await _dropLog.DeleteAsync(log.Id);

            ResultsGrid.ItemsSource = null;
            FileCountText.Text      = string.Empty;
            StatusText.Text         = "Log cleared.";
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
