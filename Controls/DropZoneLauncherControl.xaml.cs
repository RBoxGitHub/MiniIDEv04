using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MiniIDEv04.Controls
{
    public partial class DropZoneLauncherControl : UserControl, IDraggablePanel
    {
        private PanelDragBehavior? _drag;

        public static readonly DependencyProperty PanelKeyProperty =
            DependencyProperty.Register(nameof(PanelKey), typeof(string),
                typeof(DropZoneLauncherControl), new PropertyMetadata(string.Empty));
        public string PanelKey
        {
            get => (string)GetValue(PanelKeyProperty);
            set => SetValue(PanelKeyProperty, value);
        }

        public static readonly DependencyProperty PanelTitleProperty =
            DependencyProperty.Register(nameof(PanelTitle), typeof(string),
                typeof(DropZoneLauncherControl), new PropertyMetadata("📦 Drop Zone"));
        public string PanelTitle
        {
            get => (string)GetValue(PanelTitleProperty);
            set => SetValue(PanelTitleProperty, value);
        }

        public static readonly DependencyProperty TitleBarBrushProperty =
            DependencyProperty.Register(nameof(TitleBarBrush), typeof(SolidColorBrush),
                typeof(DropZoneLauncherControl),
                new PropertyMetadata(new SolidColorBrush(Color.FromArgb(255, 74, 20, 140))));
        public SolidColorBrush TitleBarBrush
        {
            get => (SolidColorBrush)GetValue(TitleBarBrushProperty);
            set => SetValue(TitleBarBrushProperty, value);
        }

        public event EventHandler<PanelPositionArgs>? PositionChanged;
        public event EventHandler<PanelPositionArgs>? DraggingPosition;
        public event EventHandler?                    PanelDoubleClicked;

        public DropZoneLauncherControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _drag = PanelDragBehavior.Attach(this, PanelKey);
            _drag.PositionChanged    += (s, a) => { ShowPos(a.Left, a.Top); PositionChanged?.Invoke(this, a); };
            _drag.DraggingPosition   += (s, a) => DraggingPosition?.Invoke(this, a);
            _drag.PanelDoubleClicked += (s, a) => PanelDoubleClicked?.Invoke(this, a);
        }

        private void ShowPos(double l, double t)
        {
            PositionLabel.Text        = $"({(int)l}, {(int)t})";
            PositionBorder.Visibility = Visibility.Visible;
        }
    }
}
