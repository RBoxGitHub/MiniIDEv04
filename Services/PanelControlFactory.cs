using MiniIDEv04.Controls;
using MiniIDEv04.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MiniIDEv04.Services
{
    /// <summary>
    /// Maps SysPanel.ControlClass string → instantiated IDraggablePanel UserControl.
    /// Add a new entry here whenever a new panel type is created.
    /// Phase 2+: could be reflection-based for plugin panels.
    /// </summary>
    public static class PanelControlFactory
    {
        /// <summary>
        /// Instantiate and configure the correct UserControl for a given SysPanel row.
        /// Returns null if ControlClass is unknown or empty.
        /// </summary>
        public static IDraggablePanel? Create(SysPanel panel)
        {
            IDraggablePanel? control = panel.ControlClass switch
            {
                "QuickAddPanelControl"       => new QuickAddPanelControl(),
                "SysManagerLauncherControl"  => new SysManagerLauncherControl(),
                "SysManagerPanelControl"     => new SysManagerPanelControl(),
                "GitHubLauncherControl"      => new GitHubLauncherControl(),
                "DropZoneLauncherControl"    => new DropZoneLauncherControl(),
                "ToolboxLauncherControl"     => new ToolboxLauncherControl(),
                "ProjectZipLauncherControl"  => new ProjectZipLauncherControl(),
                _                            => null
            };

            if (control is null) return null;
            if (control is not FrameworkElement fe) return null;

            // Set size and visibility immediately
            fe.Width          = panel.PanelWidth;
            fe.MinHeight      = panel.PanelHeight;
            fe.Visibility     = panel.IsVisible ? Visibility.Visible : Visibility.Collapsed;
            fe.IsHitTestVisible = panel.IsVisible;

            // Defer PanelKey, PanelTitle, TitleBarBrush until after
            // the control is loaded — DependencyProperty bindings need
            // the visual tree to be live before they reflect correctly
            fe.Loaded += (_, _) =>
            {
                control.PanelKey      = panel.PanelKey;
                control.PanelTitle    = panel.PanelName;
                control.TitleBarBrush = ParseBrush(panel.TitleBarColor);
            };

            return control;
        }

        private static SolidColorBrush ParseBrush(string hex)
        {
            try
            {
                return new SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter
                        .ConvertFromString(hex));
            }
            catch
            {
                return new SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(255, 55, 71, 79));
            }
        }
    }
}
