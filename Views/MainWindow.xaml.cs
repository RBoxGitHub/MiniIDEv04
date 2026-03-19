using MiniIDEv04.Controls;
using MiniIDEv04.Services;
using MiniIDEv04.ViewModels;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using Telerik.Windows.Controls;

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

            _vm.PanelManager.Panels.CollectionChanged += Panels_CollectionChanged;

            SpawnPanelsFromDb();
        }

        // ── Toolbox search ────────────────────────────────────────────

        private void ToolboxSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is RadWatermarkTextBox tb)
                _vm.FilterToolbox(tb.Text);
        }

        // ── Collection change handler ─────────────────────────────────

        private void Panels_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems is not null)
            {
                foreach (SysPanelViewModel vm in e.NewItems)
                {
                    if (!_panels.ContainsKey(vm.PanelKey))
                        SpawnSinglePanel(vm);
                }
            }
        }

        // ── Dynamic panel spawn ───────────────────────────────────────

        private void SpawnPanelsFromDb()
        {
            FloatingCanvas.Children.Clear();
            _panels.Clear();

            foreach (var panelVm in _vm.PanelManager.Panels
                .OrderBy(p => p._model.SortOrder))
            {
                SpawnSinglePanel(panelVm);
            }

            _vm.StatusMessage = $"{_vm.AppVersion} ready — {_vm.ThingsToDo.Count(t => !t.IsComplete)} open notes";
        }

        private void SpawnSinglePanel(SysPanelViewModel panelVm)
        {
            var control = PanelControlFactory.Create(panelVm._model);
            if (control is null) return;

            var fe = (UIElement)control;

            Canvas.SetLeft(fe, panelVm.PosLeft);
            Canvas.SetTop(fe, panelVm.PosTop);

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
            else if (control is ToolboxLauncherControl tbLauncher)
                tbLauncher.PanelDoubleClicked += Panel_DoubleClicked;

            if (fe is FrameworkElement fwe)
                fwe.DataContext = _vm;

            FloatingCanvas.Children.Add(fe);
            _panels[panelVm.PanelKey] = control;
        }

        // ── Panel event handlers ──────────────────────────────────────

        private async void Panel_PositionChanged(object? sender, PanelPositionArgs e)
        {
            var key = (sender as IDraggablePanel)?.PanelKey;
            if (key is null) return;
            await _vm.PanelManager.SavePositionAsync(key, e.Left, e.Top, e.Width, e.Height);
            _vm.StatusMessage = $"{_vm.AppVersion} ready — {_vm.ThingsToDo.Count(t => !t.IsComplete)} open notes";
        }

        private void Panel_DraggingPosition(object? sender, PanelPositionArgs e)
        {
            var title = (sender as IDraggablePanel)?.PanelTitle ?? "Panel";
            _vm.StatusMessage = $"📌 {title} X: {(int)e.Left} Y: {(int)e.Top}";
        }

        private void Panel_DoubleClicked(object? sender, EventArgs e)
        {
            var panel = sender as IDraggablePanel;
            if (panel is null) return;

            var target = _vm.PanelManager.GetLaunchTarget(panel.PanelKey);

            Window? win = target switch
            {
                "ThingsToDoWindow" => new ThingsToDoWindow  { Owner = this },
                "SysManagerWindow" => new SysManagerWindow  { Owner = this },
                "GitHubPushWindow" => new GitHubPushWindow  { Owner = this },
                "DropZoneWindow"   => new DropZoneWindow    { Owner = this },
                "ToolboxWindow"    => new ToolboxWindow     { Owner = this },
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
            SpawnPanelsFromDb();
            _vm.StatusMessage = "Panel layout refreshed from DB.";
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
            => Application.Current.Shutdown();
    }
}
