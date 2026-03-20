using MiniIDEv04.Data.Sqlite;
using MiniIDEv04.Models;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace MiniIDEv04.Views
{
    public partial class GitHubPushWindow : Window
    {
        private static readonly string RepoPath =
            @"D:\GrokCryptoTrack\Production-Claude\MiniIDEv04";

        private readonly SqliteGitLogRepository _gitLog = new();
        private readonly StringBuilder _logBuffer = new();

        public GitHubPushWindow()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                Log("🐙 GitHub Push ready.");
                Log($"📁 Repo: {RepoPath}");

                // Set default commit message
                if (string.IsNullOrWhiteSpace(CommitMessageBox.Text))
                    CommitMessageBox.Text = DefaultCommitMessage();
            };
        }

        private static string DefaultCommitMessage()
            => $"MiniIDEv04 — {DateTime.Now:MMM dd yyyy HH:mm:ss}";

        // ── Tab selection ─────────────────────────────────────────────────

        private async void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is TabControl tc && tc.SelectedItem is TabItem tab
                && tab.Header?.ToString() == "Git Log")
            {
                await LoadGitLogAsync();
            }
        }

        // ── Git Log ───────────────────────────────────────────────────────

        private async Task LoadGitLogAsync()
        {
            try
            {
                GitLogStatus.Text = "Loading...";
                var entries = await _gitLog.GetRecentAsync(50);
                GitLogList.ItemsSource = entries;
                GitLogStatus.Text = entries.Count == 0
                    ? "No pushes logged yet."
                    : $"{entries.Count} push(es) — most recent first";
            }
            catch (Exception ex)
            {
                GitLogStatus.Text = $"❌ Could not load log: {ex.Message}";
            }
        }

        // ── Commit + Push (header button) ─────────────────────────────────

        private async void CommitPush_Click(object sender, RoutedEventArgs e)
            => await RunFullPush();

        // ── Commit + Push (WrapPanel button) ─────────────────────────────

        private async void Push_Click(object sender, RoutedEventArgs e)
            => await RunFullPush();

        // ── Shared push logic ─────────────────────────────────────────────

        private async Task RunFullPush()
        {
            // Auto-fill message if empty
            if (string.IsNullOrWhiteSpace(CommitMessageBox.Text))
                CommitMessageBox.Text = DefaultCommitMessage();

            var message = CommitMessageBox.Text.Trim();
            var tag     = VersionTagBox.Text.Trim();

            if (!Directory.Exists(RepoPath))
            { Log($"❌  Repo path not found: {RepoPath}"); return; }

            _logBuffer.Clear();
            PushStatusBorder.Visibility = Visibility.Collapsed;

            Log("─────────────────────────────────");
            Log($"📝 Commit: {message}");
            if (!string.IsNullOrWhiteSpace(tag)) Log($"🏷  Tag: {tag}");

            await RunGitAsync("add .");
            await RunGitAsync($"commit -m \"{message}\"");
            await RunGitAsync("push origin main");

            bool success = true;
            if (!string.IsNullOrWhiteSpace(tag))
            {
                await RunGitAsync($"tag {tag}");
                var tagPush = await RunGitAsyncResult($"push origin {tag}");
                if (!tagPush) success = false;
            }

            // Log to DB
            await _gitLog.LogPushAsync(message, tag, success, _logBuffer.ToString());

            Log("✅  Done!");
            CommitMessageBox.Text       = DefaultCommitMessage();
            StatusText.Text             = "Pushed successfully.";
            PushStatusBorder.Visibility = Visibility.Visible;
            PushStatusText.Text         = $"✅  Successfully committed and pushed to GitHub!  {DateTime.Now:HH:mm:ss}";
        }

        // ── Pull ──────────────────────────────────────────────────────────

        private async void Pull_Click(object sender, RoutedEventArgs e)
        {
            PushStatusBorder.Visibility = Visibility.Collapsed;
            Log("─────────────────────────────────");
            Log("⬇  git pull origin main...");
            await RunGitAsync("pull origin main");
            Log("✅  Pull complete.");
            StatusText.Text = "Pull complete.";
        }

        // ── Git runner ────────────────────────────────────────────────────

        private async Task RunGitAsync(string args)
            => await RunGitAsyncResult(args);

        private async Task<bool> RunGitAsyncResult(string args)
        {
            try
            {
                var psi = new ProcessStartInfo("git", args)
                {
                    WorkingDirectory       = RepoPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true
                };

                using var proc = Process.Start(psi)!;
                var stdout = await proc.StandardOutput.ReadToEndAsync();
                var stderr = await proc.StandardError.ReadToEndAsync();
                await proc.WaitForExitAsync();

                if (!string.IsNullOrWhiteSpace(stdout)) Log(stdout.Trim());
                if (!string.IsNullOrWhiteSpace(stderr)) Log(stderr.Trim());

                var result = proc.ExitCode == 0
                    ? $"✓  git {args.Split(' ')[0]} OK"
                    : $"✗  git {args.Split(' ')[0]} exit {proc.ExitCode}";
                Log(result);

                _logBuffer.AppendLine(stdout);
                _logBuffer.AppendLine(stderr);
                _logBuffer.AppendLine(result);

                return proc.ExitCode == 0;
            }
            catch (Exception ex)
            {
                Log($"❌  {ex.Message}");
                _logBuffer.AppendLine(ex.Message);
                return false;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private void Log(string line)
        {
            LogOutput.Text += line + "\n";
            LogScroller.ScrollToEnd();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
