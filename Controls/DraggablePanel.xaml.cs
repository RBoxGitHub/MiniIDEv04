using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace MiniIDEv04.Controls
{
    public partial class DraggablePanel : UserControl, IDraggablePanel
    {
        private PanelDragBehavior? _drag;

        public DraggablePanel()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _drag = PanelDragBehavior.Attach(this, PanelKey);
            _drag.PositionChanged   += (s, a) => { ShowPositionLabel(a.Left, a.Top); PositionChanged?.Invoke(this, a); };
            _drag.DraggingPosition  += (s, a) => DraggingPosition?.Invoke(this, a);
            _drag.PanelDoubleClicked += (s, a) => PanelDoubleClicked?.Invoke(this, a);
        }

        // ── Dependency Properties ──────────────────────────────────────────

        public static readonly DependencyProperty PanelTitleProperty =
            DependencyProperty.Register(nameof(PanelTitle), typeof(string),
                typeof(DraggablePanel), new PropertyMetadata("Panel"));
        public string PanelTitle
        {
            get => (string)GetValue(PanelTitleProperty);
            set => SetValue(PanelTitleProperty, value);
        }

        public static readonly DependencyProperty TitleBarBrushProperty =
            DependencyProperty.Register(nameof(TitleBarBrush), typeof(SolidColorBrush),
                typeof(DraggablePanel),
                new PropertyMetadata(new SolidColorBrush(Color.FromArgb(255, 55, 71, 79))));
        public SolidColorBrush TitleBarBrush
        {
            get => (SolidColorBrush)GetValue(TitleBarBrushProperty);
            set => SetValue(TitleBarBrushProperty, value);
        }

        public static readonly DependencyProperty PanelBodyProperty =
            DependencyProperty.Register(nameof(PanelBody), typeof(UIElement),
                typeof(DraggablePanel), new PropertyMetadata(null));
        public UIElement? PanelBody
        {
            get => (UIElement?)GetValue(PanelBodyProperty);
            set => SetValue(PanelBodyProperty, value);
        }

        public static readonly DependencyProperty ShowCloseButtonProperty =
            DependencyProperty.Register(nameof(ShowCloseButton), typeof(bool),
                typeof(DraggablePanel), new PropertyMetadata(true));
        public bool ShowCloseButton
        {
            get => (bool)GetValue(ShowCloseButtonProperty);
            set => SetValue(ShowCloseButtonProperty, value);
        }

        public static readonly DependencyProperty ShowResizeGripProperty =
            DependencyProperty.Register(nameof(ShowResizeGrip), typeof(bool),
                typeof(DraggablePanel), new PropertyMetadata(true));
        public bool ShowResizeGrip
        {
            get => (bool)GetValue(ShowResizeGripProperty);
            set => SetValue(ShowResizeGripProperty, value);
        }

        public static readonly DependencyProperty PanelKeyProperty =
            DependencyProperty.Register(nameof(PanelKey), typeof(string),
                typeof(DraggablePanel), new PropertyMetadata(string.Empty));
        public string PanelKey
        {
            get => (string)GetValue(PanelKeyProperty);
            set => SetValue(PanelKeyProperty, value);
        }

        // ── Events ────────────────────────────────────────────────────────

        public event EventHandler<PanelPositionArgs>? PositionChanged;
        public event EventHandler<PanelPositionArgs>? DraggingPosition;
        public event EventHandler?                    CloseRequested;
        public event EventHandler?                    PanelDoubleClicked;

        // ── Chrome handlers ───────────────────────────────────────────────

        private void ShowPositionLabel(double left, double top)
        {
            PositionLabel.Text        = $"({(int)left}, {(int)top})";
            PositionBorder.Visibility = Visibility.Visible;
        }

        private void ResizeGrip_DragDelta(object sender, DragDeltaEventArgs e)
        {
            Width  = Math.Max(120, ActualWidth  + e.HorizontalChange);
            Height = Math.Max(80,  ActualHeight + e.VerticalChange);
        }

        private void ResizeGrip_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            var left = Canvas.GetLeft(this);
            var top  = Canvas.GetTop(this);
            ShowPositionLabel(left, top);
            PositionChanged?.Invoke(this, new PanelPositionArgs(left, top, ActualWidth, ActualHeight));
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
            => CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    public record PanelPositionArgs(double Left, double Top, double Width, double Height);
}
