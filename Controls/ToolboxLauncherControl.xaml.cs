using System.Windows;
using System.Windows.Media;

namespace MiniIDEv04.Controls
{
    public partial class ToolboxLauncherControl : System.Windows.Controls.UserControl, IDraggablePanel
    {
        private PanelDragBehavior? _drag;

        public ToolboxLauncherControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _drag = PanelDragBehavior.Attach(this, PanelKey);
            _drag.PositionChanged    += (s, a) => PositionChanged?.Invoke(this, a);
            _drag.DraggingPosition   += (s, a) => DraggingPosition?.Invoke(this, a);
            _drag.PanelDoubleClicked += (s, a) => PanelDoubleClicked?.Invoke(this, a);
        }

        // ── IDraggablePanel ───────────────────────────────────────────────

        public string PanelKey
        {
            get => (string)GetValue(PanelKeyProperty);
            set => SetValue(PanelKeyProperty, value);
        }
        public static readonly DependencyProperty PanelKeyProperty =
            DependencyProperty.Register(nameof(PanelKey), typeof(string),
                typeof(ToolboxLauncherControl), new PropertyMetadata(string.Empty));

        public string PanelTitle
        {
            get => (string)GetValue(PanelTitleProperty);
            set => SetValue(PanelTitleProperty, value);
        }
        public static readonly DependencyProperty PanelTitleProperty =
            DependencyProperty.Register(nameof(PanelTitle), typeof(string),
                typeof(ToolboxLauncherControl), new PropertyMetadata("Toolbox"));

        public SolidColorBrush TitleBarBrush
        {
            get => (SolidColorBrush)GetValue(TitleBarBrushProperty);
            set => SetValue(TitleBarBrushProperty, value);
        }
        public static readonly DependencyProperty TitleBarBrushProperty =
            DependencyProperty.Register(nameof(TitleBarBrush), typeof(SolidColorBrush),
                typeof(ToolboxLauncherControl),
                new PropertyMetadata(new SolidColorBrush(Color.FromArgb(255, 0, 96, 100))));

        // ── Events ────────────────────────────────────────────────────────

        public event EventHandler<PanelPositionArgs>? PositionChanged;
        public event EventHandler<PanelPositionArgs>? DraggingPosition;
        public event EventHandler?                    PanelDoubleClicked;
    }
}
