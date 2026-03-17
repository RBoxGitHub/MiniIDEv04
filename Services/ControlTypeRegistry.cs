using MiniIDEv04.Models;

namespace MiniIDEv04.Services
{
    /// <summary>
    /// Static registry of known control types and their designable properties.
    /// Phase 1: hand-authored for common WPF + Telerik controls.
    /// Phase 2: LibraryScanner.ScanAsync() merges reflected types in here.
    /// </summary>
    public static class ControlTypeRegistry
    {
        private static readonly List<KnownControlType> _types = new();

        static ControlTypeRegistry() => Seed();

        public static IReadOnlyList<KnownControlType> AllTypes => _types;

        public static IReadOnlyList<KnownControlType> GetByCategory(string category)
            => _types.Where(t => t.Category == category).ToList();

        public static KnownControlType? Get(string fullTypeName)
            => _types.FirstOrDefault(t => t.FullTypeName == fullTypeName);

        public static IReadOnlyList<KnownControlProperty> GetProperties(string fullTypeName)
            => Get(fullTypeName)?.Properties ?? Array.Empty<KnownControlProperty>();

        // ── Seed — Phase 1 hand-authored types ────────────────────────────

        private static void Seed()
        {
            // ── Layout props shared by all controls ──────────────────────
            var layoutProps = new[]
            {
                new KnownControlProperty("Width",              "double",    "Layout",  "Auto"),
                new KnownControlProperty("Height",             "double",    "Layout",  "Auto"),
                new KnownControlProperty("Margin",             "thickness", "Layout",  "0"),
                new KnownControlProperty("Padding",            "thickness", "Layout",  "0"),
                new KnownControlProperty("HorizontalAlignment","string",    "Layout",  "Stretch",
                    "Left | Center | Right | Stretch"),
                new KnownControlProperty("VerticalAlignment",  "string",    "Layout",  "Stretch",
                    "Top | Center | Bottom | Stretch"),
                new KnownControlProperty("Grid.Row",           "int",       "Layout",  "0"),
                new KnownControlProperty("Grid.Column",        "int",       "Layout",  "0"),
                new KnownControlProperty("Grid.RowSpan",       "int",       "Layout",  "1"),
                new KnownControlProperty("Grid.ColumnSpan",    "int",       "Layout",  "1"),
                new KnownControlProperty("Canvas.Left",        "double",    "Layout",  "0"),
                new KnownControlProperty("Canvas.Top",         "double",    "Layout",  "0"),
            };

            var visualProps = new[]
            {
                new KnownControlProperty("Background",    "color",  "Visual", "Transparent"),
                new KnownControlProperty("Foreground",    "color",  "Visual", "#FF000000"),
                new KnownControlProperty("BorderBrush",   "color",  "Visual", "Transparent"),
                new KnownControlProperty("BorderThickness","thickness","Visual","0"),
                new KnownControlProperty("FontSize",      "double", "Visual", "12"),
                new KnownControlProperty("FontWeight",    "string", "Visual", "Normal",
                    "Thin | Light | Normal | SemiBold | Bold | ExtraBold | Black"),
                new KnownControlProperty("FontFamily",    "string", "Visual", "Segoe UI"),
                new KnownControlProperty("Opacity",       "double", "Visual", "1"),
                new KnownControlProperty("IsVisible",     "bool",   "Visual", "True"),
                new KnownControlProperty("IsEnabled",     "bool",   "Visual", "True"),
            };

            // ── WPF Basic ────────────────────────────────────────────────
            Register("System.Windows.Controls.Button", "Button", "WPF", "Basic",
                layoutProps, visualProps,
                new KnownControlProperty("Content",   "string", "Content", "Button"),
                new KnownControlProperty("ToolTip",   "string", "Content", ""),
                new KnownControlProperty("Command",   "binding","Binding", "",
                    "Binding path to ICommand on DataContext"),
                new KnownControlProperty("CommandParameter","string","Binding","")
            );

            Register("System.Windows.Controls.TextBox", "TextBox", "WPF", "Input",
                layoutProps, visualProps,
                new KnownControlProperty("Text",        "string",  "Content", ""),
                new KnownControlProperty("IsReadOnly",  "bool",    "Content", "False"),
                new KnownControlProperty("MaxLength",   "int",     "Content", "0"),
                new KnownControlProperty("TextWrapping","string",  "Content", "NoWrap",
                    "NoWrap | Wrap | WrapWithOverflow"),
                new KnownControlProperty("ToolTip",    "string",   "Content", ""),
                new KnownControlProperty("Text (Binding)","binding","Binding","",
                    "Binding path to string property on DataContext")
            );

            Register("System.Windows.Controls.TextBlock", "TextBlock", "WPF", "Basic",
                layoutProps, visualProps,
                new KnownControlProperty("Text",         "string", "Content", ""),
                new KnownControlProperty("TextWrapping", "string", "Content", "NoWrap"),
                new KnownControlProperty("TextTrimming", "string", "Content", "None"),
                new KnownControlProperty("TextAlignment","string", "Content", "Left"),
                new KnownControlProperty("Text (Binding)","binding","Binding","")
            );

            Register("System.Windows.Controls.Label", "Label", "WPF", "Basic",
                layoutProps, visualProps,
                new KnownControlProperty("Content", "string", "Content", "Label"),
                new KnownControlProperty("Target",  "string", "Content", "",
                    "x:Name of the element this label is for")
            );

            Register("System.Windows.Controls.CheckBox", "CheckBox", "WPF", "Input",
                layoutProps, visualProps,
                new KnownControlProperty("Content",            "string",  "Content", "CheckBox"),
                new KnownControlProperty("IsChecked",          "bool",    "Content", "False"),
                new KnownControlProperty("IsChecked (Binding)","binding", "Binding", "")
            );

            Register("System.Windows.Controls.ComboBox", "ComboBox", "WPF", "Input",
                layoutProps, visualProps,
                new KnownControlProperty("ItemsSource (Binding)","binding","Binding",""),
                new KnownControlProperty("SelectedValue (Binding)","binding","Binding",""),
                new KnownControlProperty("DisplayMemberPath",   "string", "Content", ""),
                new KnownControlProperty("SelectedValuePath",   "string", "Content", "")
            );

            Register("System.Windows.Controls.ListBox", "ListBox", "WPF", "Data",
                layoutProps, visualProps,
                new KnownControlProperty("ItemsSource (Binding)","binding","Binding",""),
                new KnownControlProperty("SelectedItem (Binding)","binding","Binding",""),
                new KnownControlProperty("DisplayMemberPath",    "string","Content","")
            );

            Register("System.Windows.Controls.Image", "Image", "WPF", "Basic",
                layoutProps,
                new KnownControlProperty("Source",  "string","Content",""),
                new KnownControlProperty("Stretch", "string","Content","Uniform",
                    "None | Fill | Uniform | UniformToFill")
            );

            Register("System.Windows.Controls.Grid", "Grid", "WPF", "Layout",
                layoutProps, visualProps,
                new KnownControlProperty("ShowGridLines","bool","Content","False")
            );

            Register("System.Windows.Controls.StackPanel", "StackPanel", "WPF", "Layout",
                layoutProps, visualProps,
                new KnownControlProperty("Orientation","string","Content","Vertical",
                    "Vertical | Horizontal")
            );

            Register("System.Windows.Controls.Border", "Border", "WPF", "Layout",
                layoutProps, visualProps,
                new KnownControlProperty("CornerRadius",    "thickness","Content","0"),
                new KnownControlProperty("BorderBrush",     "color",    "Visual", "#FF000000"),
                new KnownControlProperty("BorderThickness", "thickness","Visual","1")
            );

            // ── Telerik ───────────────────────────────────────────────────
            Register("Telerik.Windows.Controls.RadButton", "RadButton", "Telerik", "Telerik",
                layoutProps, visualProps,
                new KnownControlProperty("Content",          "string", "Content","Button"),
                new KnownControlProperty("ToolTip",          "string", "Content",""),
                new KnownControlProperty("Command",          "binding","Binding",""),
                new KnownControlProperty("CommandParameter", "string", "Binding",""),
                new KnownControlProperty("CornerRadius",     "thickness","Visual","0")
            );

            Register("Telerik.Windows.Controls.RadTextBox", "RadTextBox", "Telerik", "Telerik",
                layoutProps, visualProps,
                new KnownControlProperty("Text",            "string", "Content",""),
                new KnownControlProperty("IsReadOnly",      "bool",   "Content","False"),
                new KnownControlProperty("Text (Binding)",  "binding","Binding",""),
                new KnownControlProperty("WatermarkContent","string", "Content","")
            );

            Register("Telerik.Windows.Controls.RadComboBox", "RadComboBox", "Telerik", "Telerik",
                layoutProps, visualProps,
                new KnownControlProperty("ItemsSource (Binding)",     "binding","Binding",""),
                new KnownControlProperty("SelectedValue (Binding)",   "binding","Binding",""),
                new KnownControlProperty("DisplayMemberPath",         "string", "Content",""),
                new KnownControlProperty("WatermarkContent",          "string", "Content","")
            );

            Register("Telerik.Windows.Controls.RadGridView", "RadGridView", "Telerik", "Telerik",
                layoutProps, visualProps,
                new KnownControlProperty("ItemsSource (Binding)",    "binding","Binding",""),
                new KnownControlProperty("AutoGenerateColumns",      "bool",   "Content","False"),
                new KnownControlProperty("IsReadOnly",               "bool",   "Content","False"),
                new KnownControlProperty("AlternateRowBackground",   "color",  "Visual", ""),
                new KnownControlProperty("AlternationCount",         "int",    "Visual", "2"),
                new KnownControlProperty("RowIndicatorVisibility",   "string", "Visual", "Collapsed"),
                new KnownControlProperty("SelectionMode",            "string", "Content","Single",
                    "Single | Multiple | Extended")
            );

            Register("Telerik.Windows.Controls.RadTreeView", "RadTreeView", "Telerik", "Telerik",
                layoutProps, visualProps,
                new KnownControlProperty("ItemsSource (Binding)",  "binding","Binding",""),
                new KnownControlProperty("DisplayMemberPath",      "string", "Content","")
            );
        }

        // ── Helper ────────────────────────────────────────────────────────

        private static void Register(
            string fullTypeName, string shortName,
            string assemblySource, string category,
            params object[] propArrays)
        {
            var props = new List<KnownControlProperty>();
            foreach (var item in propArrays)
            {
                if (item is KnownControlProperty p)
                    props.Add(p);
                else if (item is KnownControlProperty[] arr)
                    props.AddRange(arr);
            }

            _types.Add(new KnownControlType(
                fullTypeName, shortName, assemblySource, category, props));
        }
    }
}
