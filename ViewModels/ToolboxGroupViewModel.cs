using CommunityToolkit.Mvvm.ComponentModel;
using MiniIDEv04.Models;
using System.Collections.ObjectModel;

namespace MiniIDEv04.ViewModels
{
    /// <summary>
    /// ViewModel for a single toolbox group and its entries.
    /// Bound to RadPanelBarItem in Phase 2 Item 6.
    /// Supports live filtering via ApplyFilter().
    /// </summary>
    public partial class ToolboxGroupViewModel : ObservableObject
    {
        private readonly List<ToolboxEntryViewModel> _allEntries;

        public ToolboxGroupViewModel(SysToolboxGroup group, List<SysToolboxEntry> entries)
        {
            Group      = group;
            _allEntries = entries.Select(e => new ToolboxEntryViewModel(e)).ToList();
            Entries    = new ObservableCollection<ToolboxEntryViewModel>(_allEntries);
        }

        // ── Properties ────────────────────────────────────────────────

        public SysToolboxGroup Group { get; }

        public string GroupKey  => Group.GroupKey;
        public string GroupName => Group.GroupName;
        public string Icon      => Group.Icon;
        public int    Tier      => Group.Tier;

        /// <summary>Display label — icon + name.</summary>
        public string Header => $"{Icon}  {GroupName}";

        /// <summary>Filtered entries — bound to RadPanelBarItem content in Item 6.</summary>
        public ObservableCollection<ToolboxEntryViewModel> Entries { get; }

        [ObservableProperty] private bool _isExpanded = false;
        [ObservableProperty] private int  _visibleCount;

        // ── Filtering ─────────────────────────────────────────────────

        /// <summary>
        /// Filters entries by search term.
        /// Pass null or empty string to show all.
        /// Returns true if any entries match (group should be visible).
        /// </summary>
        public bool ApplyFilter(string? term)
        {
            Entries.Clear();

            IEnumerable<ToolboxEntryViewModel> filtered = string.IsNullOrWhiteSpace(term)
                ? _allEntries
                : _allEntries.Where(e =>
                    e.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    e.TypeFullName.Contains(term, StringComparison.OrdinalIgnoreCase));

            foreach (var entry in filtered)
                Entries.Add(entry);

            VisibleCount = Entries.Count;

            // Auto-expand when filtered, collapse when cleared
            if (!string.IsNullOrWhiteSpace(term) && Entries.Count > 0)
                IsExpanded = true;
            else if (string.IsNullOrWhiteSpace(term))
                IsExpanded = GroupKey is "WPF_Layout"; // default: only Layout expanded

            return Entries.Count > 0;
        }
    }

    /// <summary>
    /// ViewModel for a single toolbox entry (a control type).
    /// </summary>
    public class ToolboxEntryViewModel
    {
        public ToolboxEntryViewModel(SysToolboxEntry entry)
        {
            Entry = entry;
        }

        public SysToolboxEntry Entry { get; }

        public string DisplayName        => Entry.DisplayName;
        public string TypeFullName       => Entry.TypeFullName;
        public string AssemblyName       => Entry.AssemblyName;
        public string XmlNamespace       => Entry.XmlNamespace;
        public string DefaultXamlSnippet => Entry.DefaultXamlSnippet;
        public string Icon               => Entry.Icon;
        public int    Tier               => Entry.Tier;

        /// <summary>Display label shown in the toolbox list.</summary>
        public string Label => $"{Icon}  {DisplayName}";

        /// <summary>Tooltip shown on hover — type name + assembly.</summary>
        public string Tooltip => $"{TypeFullName}\n{AssemblyName}";
    }
}
