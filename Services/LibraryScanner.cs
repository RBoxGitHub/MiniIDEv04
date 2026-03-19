using MiniIDEv04.Data.Interfaces;
using MiniIDEv04.Data.Sqlite;
using MiniIDEv04.Models;
using System.Reflection;
using System.Windows;

namespace MiniIDEv04.Services
{
    /// <summary>
    /// Phase 2 Item 4 — Tier 1 reflection scanner.
    ///
    /// For each library in sys_Libraries where IsScanEnabled=true:
    ///   1. Loads the assembly by name (WPF built-ins) or path (drop-ins, Item 7).
    ///   2. Finds all public, concrete classes that inherit from FrameworkElement.
    ///   3. Groups them by namespace into sys_ToolboxGroups.
    ///   4. Inserts new entries into sys_ToolboxEntries (Tier=1), skipping duplicates.
    ///   5. Updates sys_Libraries.LastScannedAt.
    ///
    /// Tier 0 (seeded) entries are never touched.
    /// Progress is reported via IProgress&lt;LibraryScanProgress&gt; for UI feedback.
    /// </summary>
    public class LibraryScanner
    {
        private readonly ILibraryScannerRepository _repo;

        // WPF xmlns used for all PresentationFramework controls
        private const string WpfXmlns = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

        // Telerik xmlns
        private const string TelerikXmlns = "http://schemas.telerik.com/2008/xaml/presentation";

        // Namespaces to skip during reflection scan — too low-level or internal
        private static readonly HashSet<string> SkipNamespaces = new(StringComparer.OrdinalIgnoreCase)
        {
            "System.Windows.Documents",
            "System.Windows.Interop",
            "System.Windows.Markup",
            "System.Windows.Shell",
            "System.Windows.Threading",
        };

        // Types to skip explicitly
        private static readonly HashSet<string> SkipTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "System.Windows.Window",
            "System.Windows.Navigation.NavigationWindow",
            "System.Windows.Controls.Page",
            "System.Windows.Controls.UserControl",
        };

        public LibraryScanner(ILibraryScannerRepository? repo = null)
        {
            _repo = repo ?? new SqliteLibraryScannerRepository();
        }

        // ── Public entry point ────────────────────────────────────────────

        public async Task<LibraryScanResult> ScanAllAsync(
            IProgress<LibraryScanProgress>? progress = null,
            CancellationToken ct = default)
        {
            var result = new LibraryScanResult();
            var libraries = await _repo.GetScanEnabledAsync();

            if (libraries.Count == 0)
            {
                progress?.Report(new LibraryScanProgress("No scan-enabled libraries found.", 100));
                return result;
            }

            // Build a set of all existing TypeFullNames to avoid duplicates
            var existingTypes = await _repo.GetAllTypeNamesAsync();

            int libIndex = 0;
            foreach (var lib in libraries)
            {
                if (ct.IsCancellationRequested) break;

                int baseProgress = (int)((double)libIndex / libraries.Count * 100);
                progress?.Report(new LibraryScanProgress(
                    $"Scanning {lib.LibraryName}...", baseProgress));

                try
                {
                    var discovered = await ScanLibraryAsync(lib, existingTypes, progress, ct);
                    result.EntriesAdded   += discovered.EntriesAdded;
                    result.GroupsAdded    += discovered.GroupsAdded;
                    result.LibrariesScanned++;

                    await _repo.MarkScannedAsync(lib.Id);
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"{lib.LibraryName}: {ex.Message}");
                    progress?.Report(new LibraryScanProgress(
                        $"⚠ Error scanning {lib.LibraryName}: {ex.Message}", baseProgress));
                }

                libIndex++;
            }

            progress?.Report(new LibraryScanProgress(
                $"Scan complete — {result.EntriesAdded} controls added across {result.LibrariesScanned} libraries.",
                100));

            return result;
        }

        // ── Per-library scan ──────────────────────────────────────────────

        private async Task<LibraryScanResult> ScanLibraryAsync(
            SysLibrary lib,
            HashSet<string> existingTypes,
            IProgress<LibraryScanProgress>? progress,
            CancellationToken ct)
        {
            var result = new LibraryScanResult();

            // Load assembly
            Assembly? asm = LoadAssembly(lib);
            if (asm is null)
            {
                result.Errors.Add($"Could not load assembly: {lib.AssemblyName}");
                return result;
            }

            // Find all public concrete FrameworkElement descendants
            var controlTypes = GetControlTypes(asm);

            // Determine xmlns for this library
            string xmlns = lib.AssemblyName.StartsWith("Telerik", StringComparison.OrdinalIgnoreCase)
                ? TelerikXmlns : WpfXmlns;

            // Build prefix for XAML snippets
            string prefix = lib.AssemblyName.StartsWith("Telerik", StringComparison.OrdinalIgnoreCase)
                ? "telerik:" : "";

            // Group by namespace → toolbox group
            var byNamespace = controlTypes
                .GroupBy(t => t.Namespace ?? "Other")
                .OrderBy(g => g.Key);

            int groupSortBase = 100; // start above seeded groups
            int entrySortOrder = 0;

            foreach (var nsGroup in byNamespace)
            {
                if (ct.IsCancellationRequested) break;

                var groupKey  = $"{lib.LibraryKey}_{SanitizeKey(nsGroup.Key)}";
                var groupName = FriendlyNamespace(nsGroup.Key);

                progress?.Report(new LibraryScanProgress(
                    $"  {lib.LibraryName} → {groupName} ({nsGroup.Count()} controls)", -1));

                // Ensure group exists
                await _repo.EnsureGroupAsync(groupKey, groupName, "🔍", groupSortBase++, 1);

                // Build entries for types not already in DB
                var newEntries = new List<SysToolboxEntry>();
                entrySortOrder = 0;

                foreach (var type in nsGroup.OrderBy(t => t.Name))
                {
                    if (ct.IsCancellationRequested) break;
                    if (existingTypes.Contains(type.FullName ?? "")) continue;

                    var snippet = BuildXamlSnippet(prefix, type.Name);

                    newEntries.Add(new SysToolboxEntry
                    {
                        GroupKey         = groupKey,
                        DisplayName      = type.Name,
                        TypeFullName     = type.FullName ?? type.Name,
                        AssemblyName     = lib.AssemblyName,
                        XmlNamespace     = xmlns,
                        DefaultXamlSnippet = snippet,
                        Icon             = "🔍",
                        SortOrder        = entrySortOrder++,
                        IsVisible        = true,
                        Tier             = 1,
                        LibraryId        = lib.Id,
                    });

                    // Add to existing set to prevent duplicates within this scan run
                    existingTypes.Add(type.FullName ?? type.Name);
                }

                if (newEntries.Count > 0)
                {
                    await _repo.InsertEntriesAsync(newEntries);
                    result.EntriesAdded += newEntries.Count;
                    result.GroupsAdded++;
                }
            }

            result.LibrariesScanned = 1;
            return result;
        }

        // ── Assembly loading ──────────────────────────────────────────────

        private static Assembly? LoadAssembly(SysLibrary lib)
        {
            // Try by path first (Tier 2 DLL drop-ins — Item 7)
            if (!string.IsNullOrWhiteSpace(lib.AssemblyPath) &&
                System.IO.File.Exists(lib.AssemblyPath))
            {
                return Assembly.LoadFrom(lib.AssemblyPath);
            }

            // Try by name — works for assemblies already loaded in the AppDomain
            // (PresentationFramework, Telerik, etc. are loaded at app startup)
            var loaded = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name?.Equals(
                    lib.AssemblyName, StringComparison.OrdinalIgnoreCase) == true);

            if (loaded is not null) return loaded;

            // Last resort: try Assembly.Load by name
            try { return Assembly.Load(lib.AssemblyName); }
            catch { return null; }
        }

        // ── Type discovery ────────────────────────────────────────────────

        private static List<Type> GetControlTypes(Assembly asm)
        {
            try
            {
                return asm.GetExportedTypes()
                    .Where(t =>
                        t.IsClass &&
                        !t.IsAbstract &&
                        !t.IsGenericTypeDefinition &&
                        typeof(FrameworkElement).IsAssignableFrom(t) &&
                        !SkipNamespaces.Contains(t.Namespace ?? "") &&
                        !SkipTypes.Contains(t.FullName ?? "") &&
                        HasPublicParameterlessCtor(t))
                    .ToList();
            }
            catch (ReflectionTypeLoadException ex)
            {
                // Return whatever types did load successfully
                return ex.Types
                    .Where(t => t is not null &&
                                t.IsClass && !t.IsAbstract &&
                                !t.IsGenericTypeDefinition &&
                                typeof(FrameworkElement).IsAssignableFrom(t) &&
                                HasPublicParameterlessCtor(t))
                    .Cast<Type>()
                    .ToList();
            }
        }

        private static bool HasPublicParameterlessCtor(Type t)
        {
            try { return t.GetConstructor(Type.EmptyTypes) is not null; }
            catch { return false; }
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private static string SanitizeKey(string ns)
            => ns.Replace(".", "_").Replace(" ", "_");

        private static string FriendlyNamespace(string ns)
        {
            // "System.Windows.Controls" → "Controls"
            // "Telerik.Windows.Controls.GridView" → "GridView"
            var parts = ns.Split('.');
            return parts.Length >= 2 ? string.Join(".", parts.TakeLast(2)) : ns;
        }

        private static string BuildXamlSnippet(string prefix, string typeName)
        {
            // Reasonable default dimensions based on control name patterns
            if (typeName.Contains("Window") || typeName.Contains("Dialog"))
                return $"<{prefix}{typeName}/>";

            if (typeName.Contains("Grid") || typeName.Contains("Panel") ||
                typeName.Contains("View") || typeName.Contains("Canvas") ||
                typeName.Contains("Tree") || typeName.Contains("List"))
                return $"<{prefix}{typeName} Width=\"300\" Height=\"200\"/>";

            if (typeName.Contains("Button") || typeName.Contains("Check") ||
                typeName.Contains("Radio") || typeName.Contains("Toggle"))
                return $"<{prefix}{typeName} Content=\"{typeName}\" Width=\"100\" Height=\"30\"/>";

            if (typeName.Contains("Text") || typeName.Contains("Box") ||
                typeName.Contains("Input") || typeName.Contains("Password"))
                return $"<{prefix}{typeName} Width=\"200\" Height=\"30\"/>";

            if (typeName.Contains("Label") || typeName.Contains("Block"))
                return $"<{prefix}{typeName} Text=\"{typeName}\"/>";

            if (typeName.Contains("Bar") || typeName.Contains("Slider") ||
                typeName.Contains("Progress") || typeName.Contains("Scroll"))
                return $"<{prefix}{typeName} Width=\"200\" Height=\"20\"/>";

            // Default
            return $"<{prefix}{typeName} Width=\"100\" Height=\"30\"/>";
        }
    }

    // ── Result + progress types ───────────────────────────────────────────

    public class LibraryScanResult
    {
        public int              LibrariesScanned { get; set; }
        public int              EntriesAdded     { get; set; }
        public int              GroupsAdded      { get; set; }
        public List<string>     Errors           { get; } = new();
        public bool             HasErrors        => Errors.Count > 0;
    }

    public record LibraryScanProgress(string Message, int ProgressPercent);
}
