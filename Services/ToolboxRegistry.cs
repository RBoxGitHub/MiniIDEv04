using MiniIDEv04.Data.Interfaces;
using MiniIDEv04.Data.Sqlite;
using MiniIDEv04.ViewModels;
using System.Collections.ObjectModel;

namespace MiniIDEv04.Services
{
    /// <summary>
    /// Phase 2 Item 5 — ToolboxRegistry.
    ///
    /// Loads sys_ToolboxGroups + sys_ToolboxEntries from DB into
    /// an ObservableCollection of ToolboxGroupViewModels.
    ///
    /// Responsibilities:
    ///   - Load all groups and entries on startup / after a scan
    ///   - Expose FilteredGroups for RadPanelBar binding (Item 6)
    ///   - Apply search filter across all entries
    ///   - Reload after LibraryScanner adds new Tier 1 entries
    /// </summary>
    public class ToolboxRegistry
    {
        private readonly IToolboxRepository _repo;
        private List<ToolboxGroupViewModel> _allGroups = new();

        public ToolboxRegistry(IToolboxRepository? repo = null)
        {
            _repo = repo ?? new SqliteToolboxRepository();
        }

        // ── Public surface ────────────────────────────────────────────

        /// <summary>
        /// Filtered groups bound to RadPanelBar in Item 6.
        /// Updated by ApplyFilter() and ReloadAsync().
        /// </summary>
        public ObservableCollection<ToolboxGroupViewModel> FilteredGroups { get; }
            = new();

        /// <summary>Total number of entries across all groups.</summary>
        public int TotalEntryCount => _allGroups.Sum(g => g.Entries.Count);

        /// <summary>Total number of groups with at least one entry.</summary>
        public int GroupCount => _allGroups.Count;

        // ── Load / Reload ─────────────────────────────────────────────

        /// <summary>
        /// Loads all groups and entries from DB.
        /// Call on startup and after LibraryScanner completes.
        /// </summary>
        public async Task LoadAsync()
        {
            var groupsWithEntries = await _repo.GetGroupsWithEntriesAsync();

            _allGroups = groupsWithEntries
                .Select(t => new ToolboxGroupViewModel(t.Group, t.Entries))
                .ToList();

            // Apply default filter (no term = show all, Layout expanded)
            ApplyFilter(string.Empty);
        }

        /// <summary>
        /// Reloads from DB — called after LibraryScanner adds new entries.
        /// Preserves current filter term.
        /// </summary>
        public async Task ReloadAsync(string? currentFilter = null)
        {
            await LoadAsync();
            if (!string.IsNullOrWhiteSpace(currentFilter))
                ApplyFilter(currentFilter);
        }

        // ── Filtering ─────────────────────────────────────────────────

        /// <summary>
        /// Filters all groups by the search term.
        /// Groups with no matching entries are hidden.
        /// Pass null or empty to show all.
        /// </summary>
        public void ApplyFilter(string? term)
        {
            FilteredGroups.Clear();

            foreach (var group in _allGroups)
            {
                var hasMatches = group.ApplyFilter(term);
                if (hasMatches)
                    FilteredGroups.Add(group);
            }
        }

        // ── Entry lookup ──────────────────────────────────────────────

        /// <summary>
        /// Finds a single entry by type full name.
        /// Used by XamlRenderer (Phase 3) to get the XAML snippet.
        /// </summary>
        public ToolboxEntryViewModel? FindByTypeName(string typeFullName)
            => _allGroups
                .SelectMany(g => g.Entries)
                .FirstOrDefault(e => e.TypeFullName.Equals(
                    typeFullName, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Finds a single entry by display name.
        /// </summary>
        public ToolboxEntryViewModel? FindByDisplayName(string displayName)
            => _allGroups
                .SelectMany(g => g.Entries)
                .FirstOrDefault(e => e.DisplayName.Equals(
                    displayName, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Returns all entries across all groups as a flat list.
        /// </summary>
        public IEnumerable<ToolboxEntryViewModel> AllEntries()
            => _allGroups.SelectMany(g => g.Entries);
    }
}
