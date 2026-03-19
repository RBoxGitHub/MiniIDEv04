using MiniIDEv04.Data.Interfaces;
using MiniIDEv04.Models;
using SQLite;

namespace MiniIDEv04.Data.Sqlite
{
    public class SqliteToolboxRepository : IToolboxRepository
    {
        private readonly SQLiteAsyncConnection _db = ProjectDatabase.GetAsyncConnection();

        public Task<List<SysToolboxGroup>> GetGroupsAsync()
            => _db.Table<SysToolboxGroup>()
                  .Where(g => g.IsVisible)
                  .OrderBy(g => g.SortOrder)
                  .ToListAsync();

        public Task<List<SysToolboxEntry>> GetEntriesForGroupAsync(string groupKey)
            => _db.Table<SysToolboxEntry>()
                  .Where(e => e.GroupKey == groupKey && e.IsVisible)
                  .OrderBy(e => e.SortOrder)
                  .ToListAsync();

        public Task<List<SysToolboxEntry>> GetAllEntriesAsync()
            => _db.Table<SysToolboxEntry>()
                  .Where(e => e.IsVisible)
                  .OrderBy(e => e.SortOrder)
                  .ToListAsync();

        public async Task<List<SysToolboxEntry>> SearchEntriesAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllEntriesAsync();

            var term = searchTerm.ToLowerInvariant();
            var all  = await GetAllEntriesAsync();

            return all
                .Where(e =>
                    e.DisplayName.ToLowerInvariant().Contains(term) ||
                    e.TypeFullName.ToLowerInvariant().Contains(term))
                .ToList();
        }

        public async Task<List<(SysToolboxGroup Group, List<SysToolboxEntry> Entries)>> GetGroupsWithEntriesAsync()
        {
            var groups  = await GetGroupsAsync();
            var allEntries = await GetAllEntriesAsync();

            // Group entries by GroupKey for fast lookup
            var entryMap = allEntries
                .GroupBy(e => e.GroupKey)
                .ToDictionary(g => g.Key, g => g.OrderBy(e => e.SortOrder).ToList());

            return groups
                .Select(g => (
                    Group:   g,
                    Entries: entryMap.TryGetValue(g.GroupKey, out var entries)
                             ? entries
                             : new List<SysToolboxEntry>()))
                .Where(t => t.Entries.Count > 0)  // hide empty groups
                .ToList();
        }
    }
}
