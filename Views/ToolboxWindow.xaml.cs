using MiniIDEv04.ViewModels;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MiniIDEv04.Views
{
    /// <summary>
    /// Toolbox window — Phase 2 Items 3–7.
    /// DLL watcher status bar shows live watch state.
    /// Scan DLL button lets user manually browse + scan a DLL.
    /// Save scan log checkbox persists setting to ProjectViewModel.
    /// </summary>
    public partial class ToolboxWindow : Window
    {
        private ProjectViewModel? _vm;

        public ToolboxWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (Owner?.DataContext is ProjectViewModel vm)
            {
                _vm = vm;
                vm.PropertyChanged += Vm_PropertyChanged;

                // Bind RadPanelBar
                ToolboxPanelBar.ItemsSource = vm.ToolboxGroups;

                // Sync save log checkbox with VM
                SaveScanLogCheckBox.IsChecked = vm.SaveScanLog;

                // Show initial watcher status
                UpdateWatcherStatus();
                UpdateFooter();
            }
        }

        private void Vm_PropertyChanged(object? sender,
            System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (_vm is null) return;

            switch (e.PropertyName)
            {
                case nameof(ProjectViewModel.IsScanning):
                    ScanProgressPanel.Visibility = _vm.IsScanning
                        ? Visibility.Visible : Visibility.Collapsed;
                    ScanButton.IsEnabled    = !_vm.IsScanning;
                    ScanDllButton.IsEnabled = !_vm.IsScanning;
                    ScanButton.Content      = _vm.IsScanning
                        ? "⏳ Scanning..." : "🔍 Scan Libraries";
                    if (!_vm.IsScanning) UpdateFooter();
                    break;

                case nameof(ProjectViewModel.ScanStatus):
                    ScanStatusText.Text = _vm.ScanStatus;
                    break;

                case nameof(ProjectViewModel.ScanProgress):
                    ScanProgressBar.Value = _vm.ScanProgress;
                    break;

                case nameof(ProjectViewModel.WatcherStatus):
                    UpdateWatcherStatus();
                    break;

                case nameof(ProjectViewModel.IsWatcherActive):
                    UpdateWatcherStatus();
                    break;
            }
        }

        // ── Watcher status ────────────────────────────────────────────

        private void UpdateWatcherStatus()
        {
            if (_vm is null) return;

            WatcherStatusText.Text = _vm.WatcherStatus;
            WatcherIndicator.Fill  = _vm.IsWatcherActive
                ? new SolidColorBrush(Color.FromRgb(76, 175, 80))   // green
                : new SolidColorBrush(Color.FromRgb(158, 158, 158)); // grey
        }

        // ── Search ────────────────────────────────────────────────────

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var term = SearchBox.Text;
            Watermark.Visibility = string.IsNullOrEmpty(term)
                ? Visibility.Visible : Visibility.Collapsed;
            _vm?.FilterToolbox(term);
            UpdateFooter();
        }

        // ── Footer ────────────────────────────────────────────────────

        private void UpdateFooter()
        {
            if (_vm is null) return;

            var total  = _vm.ToolboxRegistry.TotalEntryCount;
            var groups = _vm.ToolboxRegistry.GroupCount;
            var term   = SearchBox?.Text ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(term))
            {
                var filtered = _vm.ToolboxGroups.Sum(g => g.Entries.Count);
                FooterText.Text = filtered > 0
                    ? $"🔍 {filtered} match{(filtered == 1 ? "" : "es")} across {_vm.ToolboxGroups.Count} group{(_vm.ToolboxGroups.Count == 1 ? "" : "s")}"
                    : "🔍 No matches";
            }
            else
            {
                FooterText.Text = total > 0
                    ? $"{total} controls in {groups} groups"
                    : "No controls loaded — click Scan Libraries";
            }
        }

        // ── Scan Libraries button ─────────────────────────────────────

        private void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            if (_vm is null) return;
            if (_vm.ScanLibrariesCommand.CanExecute(null))
                _ = _vm.ScanLibrariesCommand.ExecuteAsync(null);
        }

        // ── Scan DLL button ───────────────────────────────────────────

        private void ScanDllButton_Click(object sender, RoutedEventArgs e)
        {
            if (_vm is null) return;

            var dlg = new OpenFileDialog
            {
                Title            = "Select DLL to scan",
                Filter           = "DLL Files (*.dll)|*.dll",
                InitialDirectory = _vm.WatchFolder
            };

            if (dlg.ShowDialog() != true) return;

            var dllPath = dlg.FileName;

            // Show progress panel immediately
            ScanProgressPanel.Visibility = Visibility.Visible;
            ScanStatusText.Text          = $"Preparing to scan {System.IO.Path.GetFileName(dllPath)}...";
            ScanProgressBar.Value        = 0;

            // Call directly — bypasses RelayCommand parameter type complexity
            _ = _vm.ScanDllDirectAsync(dllPath);
        }

        // ── Save log checkbox ─────────────────────────────────────────

        private void SaveScanLog_Changed(object sender, RoutedEventArgs e)
        {
            if (_vm is null) return;
            _vm.SaveScanLog = SaveScanLogCheckBox.IsChecked == true;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
            => Close();

        protected override void OnClosed(EventArgs e)
        {
            if (_vm is not null)
                _vm.PropertyChanged -= Vm_PropertyChanged;
            base.OnClosed(e);
        }
    }
}
