using MiniIDEv04.Data.Sqlite;
using MiniIDEv04.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MiniIDEv04.Controls
{
    public partial class QuickAddPanelControl : UserControl, IDraggablePanel
    {
        private PanelDragBehavior? _drag;

        // ── Same DPs as DraggablePanel so MainWindow can treat them uniformly ──
        public static readonly DependencyProperty PanelKeyProperty =
            DependencyProperty.Register(nameof(PanelKey), typeof(string),
                typeof(QuickAddPanelControl), new PropertyMetadata(string.Empty));
        public string PanelKey
        {
            get => (string)GetValue(PanelKeyProperty);
            set => SetValue(PanelKeyProperty, value);
        }

        public static readonly DependencyProperty PanelTitleProperty =
            DependencyProperty.Register(nameof(PanelTitle), typeof(string),
                typeof(QuickAddPanelControl), new PropertyMetadata("Panel"));
        public string PanelTitle
        {
            get => (string)GetValue(PanelTitleProperty);
            set => SetValue(PanelTitleProperty, value);
        }

        public static readonly DependencyProperty TitleBarBrushProperty =
            DependencyProperty.Register(nameof(TitleBarBrush), typeof(SolidColorBrush),
                typeof(QuickAddPanelControl),
                new PropertyMetadata(new SolidColorBrush(Color.FromArgb(255, 55, 71, 79))));
        public SolidColorBrush TitleBarBrush
        {
            get => (SolidColorBrush)GetValue(TitleBarBrushProperty);
            set => SetValue(TitleBarBrushProperty, value);
        }

        // ── Events forwarded from behavior ────────────────────────────────
        public event EventHandler<PanelPositionArgs>? PositionChanged;
        public event EventHandler<PanelPositionArgs>? DraggingPosition;

        public QuickAddPanelControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Wire drag behavior
            _drag = PanelDragBehavior.Attach(this, PanelKey);
            _drag.PositionChanged  += (s, a) => { ShowPositionLabel(a.Left, a.Top); PositionChanged?.Invoke(this, a); };
            _drag.DraggingPosition += (s, a) => DraggingPosition?.Invoke(this, a);

            // Wire DataContext from parent Window
            var win = Window.GetWindow(this);
            if (win is not null && DataContext is null)
                DataContext = win.DataContext;
        }

        private void ShowPositionLabel(double left, double top)
        {
            PositionLabel.Text        = $"({(int)left}, {(int)top})";
            PositionBorder.Visibility = Visibility.Visible;
        }

        // ── Add button click — handled in code-behind, not via Command ────
        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as ProjectViewModel
                  ?? Window.GetWindow(this)?.DataContext as ProjectViewModel;
            if (vm is null) return;

            var text = vm.NewNoteText?.Trim();
            if (string.IsNullOrWhiteSpace(text)) return;

            var repo = new SqliteThingsToDoRepository();
            await repo.QuickAddAsync(text,
                phase:    vm.NewNotePhase,
                priority: vm.NewNotePriority);

            vm.NewNoteText   = string.Empty;
            vm.StatusMessage = $"Note saved  ·  Ph {vm.NewNotePhase}  Pri {vm.NewNotePriority}";
            await vm.LoadNotesCommand.ExecuteAsync(null);
        }
    }
}
