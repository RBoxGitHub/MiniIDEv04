using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MiniIDEv04.Controls
{
    /// <summary>
    /// Attached drag behavior for canvas panels.
    /// Drag only starts after mouse moves 4px — so single clicks
    /// on child controls (buttons, combos) pass through normally.
    /// </summary>
    public class PanelDragBehavior
    {
        private readonly FrameworkElement _element;

        private bool   _mouseDown;
        private bool   _isDragging;
        private Point  _mouseDownPos;
        private double _originLeft;
        private double _originTop;

        private const double DragThreshold = 4.0;

        public event EventHandler<PanelPositionArgs>? PositionChanged;
        public event EventHandler<PanelPositionArgs>? DraggingPosition;
        public event EventHandler?                    PanelDoubleClicked;

        public string PanelKey { get; }

        private PanelDragBehavior(FrameworkElement element, string panelKey)
        {
            _element = element;
            PanelKey = panelKey;

            element.MouseLeftButtonDown += OnMouseDown;
            element.MouseMove           += OnMouseMove;
            element.MouseLeftButtonUp   += OnMouseUp;
            element.LostMouseCapture    += OnLostCapture;

            // Double-click with handledEventsToo so it fires even if child handled
            element.AddHandler(UIElement.MouseLeftButtonDownEvent,
                new MouseButtonEventHandler(OnDoubleClick),
                handledEventsToo: true);
        }

        public static PanelDragBehavior Attach(FrameworkElement element, string panelKey)
            => new PanelDragBehavior(element, panelKey);

        private void OnDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount != 2) return;
            PanelDoubleClicked?.Invoke(_element, EventArgs.Empty);
            e.Handled = true;
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) return;           // let double-click handler deal with it
            if (_element.Parent is not Canvas canvas) return;

            // Record mouse-down position but DON'T capture yet
            // Drag only starts if mouse moves beyond DragThreshold
            _mouseDown    = true;
            _isDragging   = false;
            _mouseDownPos = e.GetPosition(canvas);
            _originLeft   = Canvas.GetLeft(_element);
            _originTop    = Canvas.GetTop(_element);

            // Capture at Element level so we get MouseMove/Up
            // but child controls still receive their own Click events
            _element.CaptureMouse();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_mouseDown || _element.Parent is not Canvas canvas) return;
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                StopDrag();
                return;
            }

            var pos   = e.GetPosition(canvas);
            var deltaX = pos.X - _mouseDownPos.X;
            var deltaY = pos.Y - _mouseDownPos.Y;

            // Only start dragging after threshold — prevents killing child clicks
            if (!_isDragging)
            {
                if (Math.Sqrt(deltaX * deltaX + deltaY * deltaY) < DragThreshold)
                    return;
                _isDragging = true;
            }

            var newLeft = Math.Max(0, Math.Min(_originLeft + deltaX,
                                               canvas.ActualWidth  - _element.ActualWidth));
            var newTop  = Math.Max(0, Math.Min(_originTop  + deltaY,
                                               canvas.ActualHeight - _element.ActualHeight));

            Canvas.SetLeft(_element, newLeft);
            Canvas.SetTop(_element,  newTop);
            DraggingPosition?.Invoke(_element,
                new PanelPositionArgs(newLeft, newTop, _element.ActualWidth, _element.ActualHeight));

            e.Handled = true;
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!_mouseDown) return;

            if (_isDragging)
            {
                var left = Canvas.GetLeft(_element);
                var top  = Canvas.GetTop(_element);
                PositionChanged?.Invoke(_element,
                    new PanelPositionArgs(left, top, _element.ActualWidth, _element.ActualHeight));
            }

            StopDrag();
            // DO NOT mark e.Handled — lets child Click events fire normally
        }

        private void OnLostCapture(object sender, MouseEventArgs e)
            => StopDrag();

        private void StopDrag()
        {
            _mouseDown  = false;
            _isDragging = false;
            if (_element.IsMouseCaptured)
                _element.ReleaseMouseCapture();
        }
    }
}
