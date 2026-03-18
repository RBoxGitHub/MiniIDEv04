using MiniIDEv04.Data.Sqlite;
using MiniIDEv04.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices;
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
            try
            {
                var root = await _settings.GetValueAsync("ProjectRootPath");
                ProjectRootBox.Text = string.IsNullOrWhiteSpace(root)
                    ? @"D:\GrokCryptoTrack\Production-Claude\MiniIDE-WorkFolder\MiniIDEv04"
                    : root;

                var folder = await _settings.GetValueAsync("ZipWorkFolder");
                _zipWorkFolder = string.IsNullOrWhiteSpace(folder)
                    ? Path.Combine(AppContext.BaseDirectory, "ZipDrop")
                    : folder;

                // Create ZipDrop folder if it doesn't exist
                Directory.CreateDirectory(_zipWorkFolder);
            }
            catch
            {
                ProjectRootBox.Text = @"D:\GrokCryptoTrack\Production-Claude\MiniIDE-WorkFolder\MiniIDEv04";
                _zipWorkFolder = Path.Combine(AppContext.BaseDirectory, "ZipDrop");
                Directory.CreateDirectory(_zipWorkFolder);
            }

            RefreshZipFolder();
            try { await LoadRecentLogAsync(); } catch { }
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
            var folder = BrowseForFolder("Select Zip Work Folder", _zipWorkFolder);
            if (folder is null) return;
            _zipWorkFolder = folder;
            await _settings.SetValueAsync("ZipWorkFolder", _zipWorkFolder);
            RefreshZipFolder();
        }

        // ── Right panel ───────────────────────────────────────────────────

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
            var folder = BrowseForFolder("Select Project Root Folder", ProjectRootBox.Text);
            if (folder is null) return;
            ProjectRootBox.Text = folder;
            await _settings.SetValueAsync("ProjectRootPath", folder);
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

        private async void ClearLog_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Clear all drop log entries?", "Confirm",
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

        // ── COM folder picker (no WinForms dependency) ────────────────────

        private static string? BrowseForFolder(string description, string? initialPath = null)
        {
            var dialog = (IFileOpenDialog)new FileOpenDialog();
            try
            {
                dialog.SetOptions(FOS.FOS_PICKFOLDERS | FOS.FOS_FORCEFILESYSTEM);
                dialog.SetTitle(description);

                if (!string.IsNullOrWhiteSpace(initialPath) && Directory.Exists(initialPath))
                {
                    var iid = typeof(IShellItem).GUID;
                    SHCreateItemFromParsingName(initialPath, IntPtr.Zero, ref iid, out var item);
                    if (item != null) dialog.SetFolder(item);
                }

                var hr = dialog.Show(IntPtr.Zero);
                if (hr != 0) return null;

                dialog.GetResult(out var result);
                result.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out var path);
                return path;
            }
            finally
            {
                Marshal.ReleaseComObject(dialog);
            }
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHCreateItemFromParsingName(
            string pszPath, IntPtr pbc, ref Guid riid, out IShellItem ppv);

        [ComImport, Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7")]
        private class FileOpenDialog { }

        [ComImport, Guid("42F85136-DB7E-439C-85F1-E4075D135FC8"),
         InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileOpenDialog
        {
            [PreserveSig] int Show(IntPtr hwnd);
            void SetFileTypes(uint cFileTypes, IntPtr rgFilterSpec);
            void SetFileTypeIndex(uint iFileType);
            void GetFileTypeIndex(out uint piFileType);
            void Advise(IntPtr pfde, out uint pdwCookie);
            void Unadvise(uint dwCookie);
            void SetOptions(FOS fos);
            void GetOptions(out FOS pfos);
            void SetDefaultFolder(IShellItem psi);
            void SetFolder(IShellItem psi);
            void GetFolder(out IShellItem ppsi);
            void GetCurrentSelection(out IShellItem ppsi);
            void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
            void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
            void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
            void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
            void GetResult(out IShellItem ppsi);
            void AddPlace(IShellItem psi, int alignment);
            void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
            void Close(int hr);
            void SetClientGuid([In] ref Guid guid);
            void ClearClientData();
            void SetFilter(IntPtr pFilter);
            void GetResults(out IntPtr ppenum);
            void GetSelectedItems(out IntPtr ppsai);
        }

        [ComImport, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"),
         InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem
        {
            void BindToHandler(IntPtr pbc, [In] ref Guid bhid,
                [In] ref Guid riid, out IntPtr ppv);
            void GetParent(out IShellItem ppsi);
            void GetDisplayName(SIGDN sigdnName,
                [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
            void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
            void Compare(IShellItem psi, uint hint, out int piOrder);
        }

        [Flags]
        private enum FOS : uint
        {
            FOS_PICKFOLDERS    = 0x00000020,
            FOS_FORCEFILESYSTEM = 0x00000040
        }

        private enum SIGDN : uint
        {
            SIGDN_FILESYSPATH = 0x80058000
        }
    }
}
