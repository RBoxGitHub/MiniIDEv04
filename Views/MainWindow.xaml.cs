using MiniIDEv04.Controls;
using MiniIDEv04.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace MiniIDEv04.Views
{
    public partial class MainWindow : Window
    {
        private ProjectViewModel _vm => (ProjectViewModel)DataContext;

        public MainWindow()
        {
            InitializeComponent();
        }

        protected override async void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            await _vm.InitializeAsync();
            RestorePanelPositions();
        }

        // ── Panel position restore ────────────────────────────────────────

        private void RestorePanelPositions()
        {
            foreach (var panelVm in _vm.PanelManager.Panels)
            {
                var panel = FindPanel(panelVm.PanelKey);
                if (panel is null) continue;

                Canvas.SetLeft((UIElement)panel, panelVm.PosLeft);
                Canvas.SetTop((UIElement)panel,  panelVm.PosTop);
                panel.Width            = panelVm.PanelWidth;
                panel.MinHeight        = panelVm.PanelHeight;
                panel.TitleBarBrush    = panelVm.TitleBarBrush;
                panel.Visibility       = panelVm.IsVisible
                    ? Visibility.Visible : Visibility.Collapsed;
                panel.IsHitTestVisible = panelVm.IsVisible;
            }
        }

        // ── Panel event handlers ──────────────────────────────────────────

        private async void Panel_PositionChanged(object sender, PanelPositionArgs e)
        {
            var key = (sender as IDraggablePanel)?.PanelKey;
            if (key is null) return;
            await _vm.PanelManager.SavePositionAsync(key, e.Left, e.Top, e.Width, e.Height);
            _vm.StatusMessage = $"{_vm.AppVersion} ready  —  {_vm.ThingsToDo.Count(t => !t.IsComplete)} open notes";
        }

        private void Panel_DraggingPosition(object sender, PanelPositionArgs e)
        {
            var title = (sender as IDraggablePanel)?.PanelTitle ?? "Panel";
            _vm.StatusMessage = $"📌  {title}   X: {(int)e.Left}   Y: {(int)e.Top}";
        }

        private async void Panel_CloseRequested(object sender, EventArgs e)
        {
            if (sender is not UIElement el) return;
            var panel = el as IDraggablePanel;
            if (panel is null) return;
            panel.Visibility       = Visibility.Collapsed;
            panel.IsHitTestVisible = false;
            await _vm.PanelManager.HideAsync(panel.PanelKey);
        }

        // ── Double-click handler ──────────────────────────────────────────

        private void Panel_DoubleClicked(object sender, EventArgs e)
        {
            var panel = sender as IDraggablePanel;
            if (panel is null) return;

            var target = _vm.PanelManager.GetLaunchTarget(panel.PanelKey);

            Window? win = target switch
            {
                "ThingsToDoWindow" => new ThingsToDoWindow { Owner = this },
                "SysManagerWindow" => new SysManagerWindow { Owner = this },
                _                  => null
            };

            if (win is null) return;
            win.ShowDialog();

            if (target == "ThingsToDoWindow")
                _ = _vm.LoadNotesCommand.ExecuteAsync(null);
            else
                _ = RefreshPanelsFromDbAsync();
        }

        private async Task RefreshPanelsFromDbAsync()
        {
            await _vm.PanelManager.LoadAsync();
            RestorePanelPositions();
            _vm.StatusMessage = "Panel layout refreshed from DB.";
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
            => Application.Current.Shutdown();

        // ── Helpers ───────────────────────────────────────────────────────

        private IDraggablePanel? FindPanel(string key) => key switch
        {
            "QuickAddPanel"      => QuickAddPanel,
            "SysManagerLauncher" => SysManagerLauncher,
            "SysManagerPanel"    => SysManagerPanel,
            _                    => null
        };
    }
}
