using System.Windows;
using System.Windows.Media;

namespace MiniIDEv04.Controls
{
    /// <summary>
    /// Common interface implemented by DraggablePanel and QuickAddPanelControl
    /// so MainWindow can treat both types uniformly for position restore,
    /// event handling, and drag status display.
    /// </summary>
    public interface IDraggablePanel
    {
        string         PanelKey     { get; set; }
        string         PanelTitle   { get; set; }
        SolidColorBrush TitleBarBrush { get; set; }
        double         Width        { get; set; }
        double         MinHeight    { get; set; }
        Visibility     Visibility   { get; set; }
        bool           IsHitTestVisible { get; set; }

        event EventHandler<PanelPositionArgs>? PositionChanged;
        event EventHandler<PanelPositionArgs>? DraggingPosition;
    }
}
