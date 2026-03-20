using MiniIDEv04.Data.Interfaces;
using System.Text.RegularExpressions;

namespace MiniIDEv04.Services
{
    /// <summary>
    /// Builds an xmlns prefix map from sys_ToolboxEntries.XmlNamespace values
    /// and injects the correct xmlns declarations into a XAML snippet before rendering.
    /// Phase 3 — Priority 1.
    /// </summary>
    public class XmlnsInjector
    {
        private readonly IToolboxRepository _toolboxRepository;

        // Well-known namespace → preferred prefix mappings
        private static readonly Dictionary<string, string> _knownPrefixes = new()
        {
            ["http://schemas.microsoft.com/winfx/2006/xaml/presentation"] = "wpf",
            ["http://schemas.microsoft.com/winfx/2006/xaml"]              = "x",
            ["http://schemas.telerik.com/2008/xaml/presentation"]         = "telerik",
            ["http://schemas.microsoft.com/expression/blend/2008"]        = "d",
            ["http://schemas.openxmlformats.org/markup-compatibility/2006"] = "mc",
        };

        public XmlnsInjector(IToolboxRepository toolboxRepository)
        {
            _toolboxRepository = toolboxRepository;
        }

        // ─────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Builds the full xmlns map by querying all distinct XmlNamespace
        /// values from sys_ToolboxEntries, then merging with well-known prefixes.
        /// Returns prefix → namespaceUri dictionary.
        /// </summary>
        public async Task<Dictionary<string, string>> BuildXmlnsMapAsync()
        {
            var entries = await _toolboxRepository.GetAllEntriesAsync();

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Seed with well-known prefixes first
            foreach (var (ns, prefix) in _knownPrefixes)
                map[prefix] = ns;

            // Walk all entries and assign prefixes for any unknown namespaces
            int autoIndex = 1;
            foreach (var entry in entries)
            {
                var ns = entry.XmlNamespace?.Trim();
                if (string.IsNullOrEmpty(ns)) continue;

                // Already covered by a known prefix?
                if (_knownPrefixes.ContainsKey(ns)) continue;

                // Already added in a prior iteration?
                if (map.ContainsValue(ns)) continue;

                // Generate a safe prefix from the namespace URI
                string prefix = DerivePrefix(ns, map, ref autoIndex);
                map[prefix] = ns;
            }

            return map;
        }

        /// <summary>
        /// Injects xmlns declarations into the root element of the supplied XAML snippet.
        /// Only namespaces not already declared on the root element are added.
        /// Returns the modified XAML string ready for XamlReader.Load.
        /// </summary>
        public async Task<string> InjectXmlnsAsync(string xaml)
        {
            if (string.IsNullOrWhiteSpace(xaml))
                return xaml;

            var map = await BuildXmlnsMapAsync();
            return InjectXmlns(xaml, map);
        }

        /// <summary>
        /// Synchronous overload — injects from a pre-built map.
        /// Use this inside XamlRenderer so the map is built once per render session.
        /// </summary>
        public static string InjectXmlns(string xaml, Dictionary<string, string> xmlnsMap)
        {
            if (string.IsNullOrWhiteSpace(xaml))
                return xaml;

            // Find the opening tag of the root element  e.g.  <Button   or  <Grid
            var rootTagMatch = Regex.Match(xaml, @"<([A-Za-z][\w\.:]*)", RegexOptions.None);
            if (!rootTagMatch.Success)
                return xaml; // Not valid XML — return as-is, let XamlRenderer handle it

            // Collect xmlns declarations already present in the document
            var existingNs = new HashSet<string>(
                Regex.Matches(xaml, @"xmlns(?::\w+)?\s*=\s*""([^""]+)""")
                     .Select(m => m.Groups[1].Value),
                StringComparer.OrdinalIgnoreCase);

            // Build the injection string for missing namespaces
            var injections = new List<string>();
            foreach (var (prefix, ns) in xmlnsMap)
            {
                if (existingNs.Contains(ns)) continue;

                // Default namespace uses xmlns="..." , others use xmlns:prefix="..."
                string attr = prefix == "wpf"
                    ? $"""xmlns="{ns}" """
                    : $"""xmlns:{prefix}="{ns}" """;

                injections.Add(attr.TrimEnd());
            }

            if (injections.Count == 0)
                return xaml; // Nothing to inject

            // Insert injections just after the root element name
            // e.g.  <Grid  →  <Grid xmlns="..." xmlns:x="..."
            int insertAt = rootTagMatch.Index + rootTagMatch.Length;
            string injected = " " + string.Join(" ", injections);

            return xaml.Insert(insertAt, injected);
        }

        // ─────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Derives a short, collision-free XML prefix from a namespace URI.
        /// e.g. "http://schemas.myco.com/controls/v2" → "controls"
        /// Falls back to "ns1", "ns2" … if the derived name collides.
        /// </summary>
        private static string DerivePrefix(string ns,
                                           Dictionary<string, string> existing,
                                           ref int autoIndex)
        {
            // Try last meaningful path segment of the URI
            string candidate = ns.TrimEnd('/').Split('/', '\\', '.', ':')
                                  .LastOrDefault(s => s.Length > 1 && !s.All(char.IsDigit))
                               ?? "ns";

            // Sanitise — XML prefix must match [A-Za-z_][\w.-]*
            candidate = Regex.Replace(candidate, @"[^\w]", "_").ToLowerInvariant();
            if (!char.IsLetter(candidate[0]) && candidate[0] != '_')
                candidate = "_" + candidate;

            // Ensure uniqueness
            string final = candidate;
            while (existing.ContainsKey(final))
                final = candidate + autoIndex++;

            return final;
        }
    }
}
