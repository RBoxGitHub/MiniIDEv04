using MiniIDEv04.Models;

namespace MiniIDEv04.Data.Interfaces
{
    /// <summary>
    /// Repository interface for toolbox group and entry read operations.
    /// Write operations (scan results) go through ILibraryScannerRepository.
    /// </summary>
    public interface IToolboxRepository
    {
        /// <summary>Returns all visible toolbox groups ordered by SortOrder.</summary>
        Task<List<SysToolboxGroup>> GetGroupsAsync();

        /// <summary>Returns all visible entries for a specific group key.</summary>
        Task<List<SysToolboxEntry>> GetEntriesForGroupAsync(string groupKey);

        /// <summary>Returns all visible entries across all groups.</summary>
        Task<List<SysToolboxEntry>> GetAllEntriesAsync();

        /// <summary>
        /// Returns entries matching the search term across DisplayName and TypeFullName.
        /// </summary>
        Task<List<SysToolboxEntry>> SearchEntriesAsync(string searchTerm);

        /// <summary>Returns all groups with their entries in a single query pass.</summary>
        Task<List<(SysToolboxGroup Group, List<SysToolboxEntry> Entries)>> GetGroupsWithEntriesAsync();
    }
}
