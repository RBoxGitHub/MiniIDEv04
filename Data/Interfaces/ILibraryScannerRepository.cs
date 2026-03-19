using MiniIDEv04.Models;

namespace MiniIDEv04.Data.Interfaces
{
    /// <summary>
    /// Repository interface for LibraryScanner read/write operations.
    /// Keeps all DB access out of the scanner service itself.
    /// </summary>
    public interface ILibraryScannerRepository
    {
        /// <summary>Returns all libraries where IsScanEnabled = true.</summary>
        Task<List<SysLibrary>> GetScanEnabledAsync();

        /// <summary>Returns all existing Tier 1+ entries for a given group key.</summary>
        Task<List<SysToolboxEntry>> GetScannedEntriesAsync(string groupKey);

        /// <summary>Returns all existing entry TypeFullNames for fast duplicate checking.</summary>
        Task<HashSet<string>> GetAllTypeNamesAsync();

        /// <summary>Inserts a batch of new toolbox entries.</summary>
        Task InsertEntriesAsync(IEnumerable<SysToolboxEntry> entries);

        /// <summary>
        /// Ensures a toolbox group exists for the given key.
        /// Inserts it if missing, returns existing if present.
        /// </summary>
        Task<SysToolboxGroup> EnsureGroupAsync(string groupKey, string groupName,
            string icon, int sortOrder, int tier);

        /// <summary>Updates LastScannedAt for the given library.</summary>
        Task MarkScannedAsync(int libraryId);

        /// <summary>Deletes all Tier 1 entries (used before a full rescan).</summary>
        Task ClearTier1EntriesAsync();
    }
}
