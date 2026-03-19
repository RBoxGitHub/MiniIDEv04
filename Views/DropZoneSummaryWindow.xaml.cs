using MiniIDEv04.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace MiniIDEv04.Views
{
    public partial class DropZoneSummaryWindow : Window
    {
        private readonly string _projectRoot;
        private readonly string _commitMessage;

        // View model for the file grid
        private record FileRow(string StatusIcon, string FileName, string Destination);

        public DropZoneSummaryWindow(
            string zipName,
            List<ZipDropService.DropResult> results,
            string projectRoot,
            string commitMessage = "")
        {
            InitializeComponent();

            _projectRoot   = projectRoot;
            _commitMessage = commitMessage;

            LoadSummary(zipName, results, commitMessage);
        }

        private void LoadSummary(
            string zipName,
            List<ZipDropService.DropResult> results,
            string commitMessage)
        {
            // Header
            ZipNameText.Text = $"{zipName}  ·  {DateTime.Now:MMM dd yyyy  HH:mm:ss}";

            // Stat boxes
            TotalCount.Text   = results.Count.ToString();
            NewCount.Text     = results.Count(r => r.Status == "New").ToString();
            UpdatedCount.Text = results.Count(r => r.Status == "Updated").ToString();
            SkippedCount.Text = results.Count(r => r.Status.StartsWith("Skipped")).ToString();
            ErrorCount.Text   = results.Count(r => r.Status.StartsWith("Error")).ToString();

            // Commit message
            CommitMessageBox.Text = string.IsNullOrWhiteSpace(commitMessage)
                ? "(no commit message in manifest)"
                : commitMessage;

            // File grid
            var rows = results.Select(r => new FileRow(
                StatusIcon: r.Status switch
                {
                    "New"     => "✅ New",
                    "Updated" => "🔄 Updated",
                    var s when s.StartsWith("Skipped") => "⏭ Skipped",
                    var s when s.StartsWith("Error")   => "❌ Error",
                    _                                  => r.Status
                },
                FileName:    r.FileName,
                Destination: r.Destination
            )).ToList();

            FileGrid.ItemsSource = new ObservableCollection<FileRow>(rows);

            // Footer
            var errorCount = results.Count(r => r.Status.StartsWith("Error"));
            FooterText.Text = errorCount > 0
                ? $"⚠ {errorCount} file(s) had errors — check the list above"
                : $"✅ All files deployed successfully";
        }

        // ── Copy commit message ───────────────────────────────────────
        private void CopyCommit_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(CommitMessageBox.Text))
            {
                Clipboard.SetText(CommitMessageBox.Text);
                FooterText.Text = "✅ Commit message copied to clipboard";
            }
        }

        // ── Build Now ─────────────────────────────────────────────────
        private async void BuildButton_Click(object sender, RoutedEventArgs e)
        {
            BuildButton.IsEnabled  = false;
            BuildButton.Content    = "⏳ Building...";
            BuildStatusBorder.Visibility = Visibility.Visible;
            BuildStatusText.Foreground   = new SolidColorBrush(Color.FromRgb(51, 105, 30));
            BuildStatusText.Text   = "Running dotnet build...";

            var csprojPath = FindCsproj(_projectRoot);
            if (csprojPath is null)
            {
                BuildStatusText.Text       = "❌ Could not find .csproj in project root.";
                BuildStatusText.Foreground = Brushes.Red;
                BuildButton.IsEnabled      = true;
                BuildButton.Content        = "🔨 Build Now";
                return;
            }

            var (success, output) = await RunBuildAsync(csprojPath);

            BuildStatusText.Text = success
                ? "✅ Build succeeded"
                : $"❌ Build failed — see DropZone build output panel for details";

            BuildStatusText.Foreground = success
                ? new SolidColorBrush(Color.FromRgb(51, 105, 30))
                : Brushes.Red;

            BuildStatusBorder.Background = success
                ? new SolidColorBrush(Color.FromRgb(241, 248, 233))
                : new SolidColorBrush(Color.FromRgb(255, 235, 238));

            BuildButton.IsEnabled = true;
            BuildButton.Content   = "🔨 Build Again";

            // Pass build output back to DropZoneWindow via tag
            Tag = output;
        }

        private static string? FindCsproj(string projectRoot)
        {
            if (!Directory.Exists(projectRoot)) return null;
            var files = Directory.GetFiles(projectRoot, "*.csproj", SearchOption.TopDirectoryOnly);
            return files.FirstOrDefault();
        }

        private static async Task<(bool Success, string Output)> RunBuildAsync(string csprojPath)
        {
            var sb = new System.Text.StringBuilder();

            var psi = new ProcessStartInfo
            {
                FileName               = "dotnet",
                Arguments              = $"build \"{csprojPath}\" --nologo -v minimal",
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                CreateNoWindow         = true,
                WorkingDirectory       = Path.GetDirectoryName(csprojPath)!
            };

            try
            {
                using var process = new Process { StartInfo = psi };
                process.Start();

                var stdOut = await process.StandardOutput.ReadToEndAsync();
                var stdErr = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                sb.AppendLine(stdOut);
                if (!string.IsNullOrWhiteSpace(stdErr))
                    sb.AppendLine(stdErr);

                return (process.ExitCode == 0, sb.ToString());
            }
            catch (Exception ex)
            {
                return (false, $"Failed to start build process: {ex.Message}");
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
