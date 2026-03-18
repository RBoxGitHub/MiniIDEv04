using MiniIDEv04.Controls;
using MiniIDEv04.Services;
using MiniIDEv04.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace MiniIDEv04.Views
{
    public partial class MainWindow : Window
    {
        private ProjectViewModel _vm => (ProjectViewModel)DataContext;

        private readonly Dictionary<string, IDraggablePanel> _panels = new();

        public MainWindow()
        {
            InitializeComponent();
        }

        protected override async void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            await _vm.InitializeAsync();
            SpawnPanelsFromDb();
        }

        // ── Dynamic panel spawn ───────────────────────────────────────────

        private void SpawnPanelsFromDb()
        {
            FloatingCanvas.Children.Clear();
            _panels.Clear();

            // Use _model to get SortOrder and full SysPanel data
            foreach (var panelVm in _vm.PanelManager.Panels
                         .OrderBy(p => p._model.SortOrder))
            {
                var control = PanelControlFactory.Create(panelVm._model);
                if (control is null) continue;

                var fe = (UIElement)control;
                Canvas.SetLeft(fe, panelVm.PosLeft);
                Canvas.SetTop(fe,  panelVm.PosTop);

                control.PositionChanged  += Panel_PositionChanged;
                control.DraggingPosition += Panel_DraggingPosition;

                if (control is SysManagerLauncherControl launcher)
                    launcher.PanelDoubleClicked += Panel_DoubleClicked;
                else if (control is SysManagerPanelControl sysPanel)
                    sysPanel.PanelDoubleClicked += Panel_DoubleClicked;
                else if (control is GitHubLauncherControl ghLauncher)
                    ghLauncher.PanelDoubleClicked += Panel_DoubleClicked;
                else if (control is DropZoneLauncherControl dzLauncher)
                    dzLauncher.PanelDoubleClicked += Panel_DoubleClicked;

                if (fe is FrameworkElement fwe)
                    fwe.DataContext = _vm;

                FloatingCanvas.Children.Add(fe);
                _panels[panelVm.PanelKey] = control;
            }

            _vm.StatusMessage = $"{_vm.AppVersion} ready  —  {_vm.ThingsToDo.Count(t => !t.IsComplete)} open notes";
        }

        // ── Panel event handlers ──────────────────────────────────────────

        private async void Panel_PositionChanged(object? sender, PanelPositionArgs e)
        {
            var key = (sender as IDraggablePanel)?.PanelKey;
            if (key is null) return;
            await _vm.PanelManager.SavePositionAsync(key, e.Left, e.Top, e.Width, e.Height);
            _vm.StatusMessage = $"{_vm.AppVersion} ready  —  {_vm.ThingsToDo.Count(t => !t.IsComplete)} open notes";
        }

        private void Panel_DraggingPosition(object? sender, PanelPositionArgs e)
        {
            var title = (sender as IDraggablePanel)?.PanelTitle ?? "Panel";
            _vm.StatusMessage = $"📌  {title}   X: {(int)e.Left}   Y: {(int)e.Top}";
        }

        private void Panel_DoubleClicked(object? sender, EventArgs e)
        {
            var panel = sender as IDraggablePanel;
            if (panel is null) return;

            var target = _vm.PanelManager.GetLaunchTarget(panel.PanelKey);

            Window? win = target switch
            {
                "ThingsToDoWindow"  => new ThingsToDoWindow  { Owner = this },
                "SysManagerWindow"  => new SysManagerWindow  { Owner = this },
                "GitHubPushWindow"  => new GitHubPushWindow  { Owner = this },
                "DropZoneWindow"    => new DropZoneWindow    { Owner = this },
                _                   => null
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
            SpawnPanelsFromDb();
            _vm.StatusMessage = "Panel layout refreshed from DB.";
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
            => Application.Current.Shutdown();
    }
}
