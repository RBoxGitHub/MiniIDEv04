using MiniIDEv04.Data.Interfaces;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace MiniIDEv04.Services
{
    /// <summary>
    /// Two-pass XAML renderer for the PreviewHost ContentPresenter.
    /// Pass 1 — inject xmlns map and attempt XamlReader.Load directly.
    /// Pass 2 — wrap snippet in a neutral root container and retry.
    /// Never throws to the caller — returns a styled error panel on failure.
    /// Phase 3 — Priority 1.
    /// </summary>
    public class XamlRenderer
    {
        private readonly XmlnsInjector _injector;

        // Cached xmlns map — built once on first render, reused thereafter.
        private Dictionary<string, string>? _xmlnsMap;

        public XamlRenderer(IToolboxRepository toolboxRepository)
        {
            _injector = new XmlnsInjector(toolboxRepository);
        }

        // ─────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Renders a XAML snippet to a UIElement ready for PreviewHost.Content.
        /// Returns a styled error panel if both passes fail.
        /// Must be called on the UI thread (XamlReader.Load requirement).
        /// </summary>
        public async Task<UIElement> RenderAsync(string xaml)
        {
            if (string.IsNullOrWhiteSpace(xaml))
                return MakeErrorPanel("No XAML to render.", detail: null);

            // Build xmlns map once and cache it
            _xmlnsMap ??= await _injector.BuildXmlnsMapAsync();

            // ── Pass 1: inject xmlns and load ────────────────────────
            try
            {
                string injected = XmlnsInjector.InjectXmlns(xaml, _xmlnsMap);
                var element = LoadXaml(injected);
                if (element is UIElement ui) return ui;

                return MakeErrorPanel(
                    "XAML loaded but result is not a UIElement.",
                    detail: $"Actual type: {element?.GetType().FullName ?? "null"}");
            }
            catch (Exception pass1Ex)
            {
                // ── Pass 2: wrap in neutral root and retry ────────────
                try
                {
                    string wrapped  = WrapInRoot(xaml, _xmlnsMap);
                    var    element  = LoadXaml(wrapped);

                    // Unwrap single child if the root is our synthetic Border
                    if (element is Border border && border.Tag?.ToString() == "XamlRendererRoot")
                        return border.Child as UIElement ?? border;

                    if (element is UIElement ui2) return ui2;

                    return MakeErrorPanel(
                        "Pass 2 loaded but result is not a UIElement.",
                        detail: $"Actual type: {element?.GetType().FullName ?? "null"}");
                }
                catch (Exception pass2Ex)
                {
                    return MakeErrorPanel(
                        "XAML could not be rendered.",
                        detail: $"Pass 1: {pass1Ex.Message}\nPass 2: {pass2Ex.Message}");
                }
            }
        }

        /// <summary>
        /// Clears the cached xmlns map — call after a toolbox reload so new
        /// namespaces are picked up on the next render.
        /// </summary>
        public void InvalidateXmlnsCache() => _xmlnsMap = null;

        // ─────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Calls XamlReader.Load on the supplied string.
        /// Must run on the UI thread.
        /// </summary>
        private static object? LoadXaml(string xaml)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xaml));
            return XamlReader.Load(stream);
        }

        /// <summary>
        /// Pass 2 fallback — wraps the raw snippet inside a Border that
        /// carries all xmlns declarations, giving XamlReader a valid root.
        /// </summary>
        private static string WrapInRoot(string xaml, Dictionary<string, string> map)
        {
            var sb = new StringBuilder();
            sb.Append("<Border xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"");
            sb.Append(" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"");

            foreach (var (prefix, ns) in map)
            {
                // Skip the two we already emitted above
                if (prefix is "wpf" or "x") continue;
                sb.Append($" xmlns:{prefix}=\"{ns}\"");
            }

            sb.Append(" Tag=\"XamlRendererRoot\">");
            sb.Append(xaml.Trim());
            sb.Append("</Border>");
            return sb.ToString();
        }

        /// <summary>
        /// Builds a styled error panel shown in PreviewHost when rendering fails.
        /// </summary>
        private static UIElement MakeErrorPanel(string message, string? detail)
        {
            var stack = new StackPanel { Margin = new Thickness(8) };

            stack.Children.Add(new TextBlock
            {
                Text       = "⚠ Render Error",
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xB7, 0x1C, 0x1C)),
                FontSize   = 13,
                Margin     = new Thickness(0, 0, 0, 4)
            });

            stack.Children.Add(new TextBlock
            {
                Text         = message,
                Foreground   = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
                FontSize     = 11,
                TextWrapping = TextWrapping.Wrap
            });

            if (!string.IsNullOrWhiteSpace(detail))
            {
                stack.Children.Add(new TextBlock
                {
                    Text         = detail,
                    Foreground   = new SolidColorBrush(Color.FromRgb(0x75, 0x75, 0x75)),
                    FontSize     = 10,
                    TextWrapping = TextWrapping.Wrap,
                    Margin       = new Thickness(0, 4, 0, 0),
                    FontFamily   = new FontFamily("Consolas")
                });
            }

            return new Border
            {
                Background      = new SolidColorBrush(Color.FromRgb(0xFF, 0xEB, 0xEB)),
                BorderBrush     = new SolidColorBrush(Color.FromRgb(0xEF, 0x9A, 0x9A)),
                BorderThickness = new Thickness(1),
                CornerRadius    = new CornerRadius(4),
                Padding         = new Thickness(8),
                Child           = stack
            };
        }
    }
}
