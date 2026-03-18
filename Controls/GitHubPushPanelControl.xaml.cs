using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MiniIDEv04.Controls
{
    public partial class GitHubPushPanelControl : UserControl, IDraggablePanel
    {
        private PanelDragBehavior? _drag;

        private static readonly string DefaultRepoPath =
            @"D:\GrokCryptoTrack\Production-Claude\MiniIDEv04";

        // ── IDraggablePanel DPs ───────────────────────────────────────────

        public static readonly DependencyProperty PanelKeyProperty =
            DependencyProperty.Register(nameof(PanelKey), typeof(string),
                typeof(GitHubPushPanelControl), new PropertyMetadata(string.Empty));
        public string PanelKey
        {
            get => (string)GetValue(PanelKeyProperty);
            set => SetValue(PanelKeyProperty, value);
        }

        public static readonly DependencyProperty PanelTitleProperty =
            DependencyProperty.Register(nameof(PanelTitle), typeof(string),
                typeof(GitHubPushPanelControl), new PropertyMetadata("🐙 GitHub Push"));
        public string PanelTitle
        {
            get => (string)GetValue(PanelTitleProperty);
            set => SetValue(PanelTitleProperty, value);
        }

        public static readonly DependencyProperty TitleBarBrushProperty =
            DependencyProperty.Register(nameof(TitleBarBrush), typeof(SolidColorBrush),
                typeof(GitHubPushPanelControl),
                new PropertyMetadata(new SolidColorBrush(Color.FromArgb(255, 27, 94, 32))));
        public SolidColorBrush TitleBarBrush
        {
            get => (SolidColorBrush)GetValue(TitleBarBrushProperty);
            set => SetValue(TitleBarBrushProperty, value);
        }

        public event EventHandler<PanelPositionArgs>? PositionChanged;
        public event EventHandler<PanelPositionArgs>? DraggingPosition;

        public GitHubPushPanelControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _drag = PanelDragBehavior.Attach(this, PanelKey);
            _drag.PositionChanged  += (s, a) => { ShowPos(a.Left, a.Top); PositionChanged?.Invoke(this, a); };
            _drag.DraggingPosition += (s, a) => { ShowPos(a.Left, a.Top); DraggingPosition?.Invoke(this, a); };

            Log("🐙 GitHub Push panel ready.");
            Log($"📁 Repo: {DefaultRepoPath}");
        }

        private void ShowPos(double l, double t)
            => PositionLabel.Text = $"({(int)l}, {(int)t})";

        // ── Quick Pull button ─────────────────────────────────────────────

        private async void PullButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(DefaultRepoPath))
            { Log($"❌  Repo path not found: {DefaultRepoPath}"); return; }

            Log("─────────────────────────────────");
            Log("⬇  Running git pull origin main...");
            await RunGitAsync("pull origin main", DefaultRepoPath);
            Log("✅  Pull complete.");
        }

        // ── Quick Push button (no commit — just push) ─────────────────────

        private async void QuickPushButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(DefaultRepoPath))
            { Log($"❌  Repo path not found: {DefaultRepoPath}"); return; }

            Log("─────────────────────────────────");
            Log("⬆  Running git push origin main...");
            await RunGitAsync("push origin main", DefaultRepoPath);
            Log("✅  Push complete.");
        }

        // ── Commit + Push button ──────────────────────────────────────────

        private async void PushButton_Click(object sender, RoutedEventArgs e)
        {
            var message = CommitMessageBox.Text.Trim();
            var tag     = VersionTagBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(message))
            { Log("⚠  Enter a commit message first."); return; }

            if (!Directory.Exists(DefaultRepoPath))
            { Log($"❌  Repo path not found: {DefaultRepoPath}"); return; }

            Log("─────────────────────────────────");
            Log($"📝 Commit: {message}");
            if (!string.IsNullOrWhiteSpace(tag)) Log($"🏷  Tag: {tag}");

            await RunGitAsync("add .", DefaultRepoPath);
            await RunGitAsync($"commit -m \"{message}\"", DefaultRepoPath);
            await RunGitAsync("push origin main", DefaultRepoPath);

            if (!string.IsNullOrWhiteSpace(tag))
            {
                await RunGitAsync($"tag {tag}", DefaultRepoPath);
                await RunGitAsync($"push origin {tag}", DefaultRepoPath);
            }

            Log("✅  Done!");
            CommitMessageBox.Text = string.Empty;
        }

        // ── Git runner ────────────────────────────────────────────────────

        private async Task RunGitAsync(string args, string workingDir)
        {
            try
            {
                var psi = new ProcessStartInfo("git", args)
                {
                    WorkingDirectory       = workingDir,
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

                Log(proc.ExitCode == 0
                    ? $"✓  git {args.Split(' ')[0]} OK"
                    : $"✗  git {args.Split(' ')[0]} exit {proc.ExitCode}");
            }
            catch (Exception ex)
            {
                Log($"❌  {ex.Message}");
            }
        }

        private void Log(string line)
        {
            LogOutput.Text += line + "\n";
            LogScroller.ScrollToEnd();
        }
    }
}
