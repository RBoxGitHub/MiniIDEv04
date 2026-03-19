using MiniIDEv04.Data.Interfaces;
using MiniIDEv04.Models;
using SQLite;

namespace MiniIDEv04.Data.Sqlite
{
    public class SqliteLibraryScannerRepository : ILibraryScannerRepository
    {
        private readonly SQLiteAsyncConnection _db = ProjectDatabase.GetAsyncConnection();

        public Task<List<SysLibrary>> GetScanEnabledAsync()
            => _db.Table<SysLibrary>()
                  .Where(l => l.IsScanEnabled && l.IsVisible)
                  .ToListAsync();

        public Task<List<SysToolboxEntry>> GetScannedEntriesAsync(string groupKey)
            => _db.Table<SysToolboxEntry>()
                  .Where(e => e.GroupKey == groupKey && e.Tier >= 1)
                  .ToListAsync();

        public async Task<HashSet<string>> GetAllTypeNamesAsync()
        {
            var all = await _db.Table<SysToolboxEntry>().ToListAsync();
            return all.Select(e => e.TypeFullName).ToHashSet();
        }

        public async Task InsertEntriesAsync(IEnumerable<SysToolboxEntry> entries)
        {
            foreach (var entry in entries)
            {
                entry.CreatedAt = entry.UpdatedAt = DateTime.UtcNow;
                await _db.InsertAsync(entry);
            }
        }

        public async Task<SysToolboxGroup> EnsureGroupAsync(
            string groupKey, string groupName, string icon, int sortOrder, int tier)
        {
            var existing = await _db.Table<SysToolboxGroup>()
                                    .Where(g => g.GroupKey == groupKey)
                                    .FirstOrDefaultAsync();
            if (existing is not null) return existing;

            var group = new SysToolboxGroup
            {
                GroupKey  = groupKey,
                GroupName = groupName,
                Icon      = icon,
                SortOrder = sortOrder,
                Tier      = tier,
                IsVisible = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            await _db.InsertAsync(group);
            return group;
        }

        public async Task MarkScannedAsync(int libraryId)
        {
            await _db.ExecuteAsync(
                "UPDATE sys_Libraries SET LastScannedAt=?, UpdatedAt=? WHERE Id=?",
                DateTime.UtcNow, DateTime.UtcNow, libraryId);
        }

        public Task ClearTier1EntriesAsync()
            => _db.ExecuteAsync(
                "DELETE FROM sys_ToolboxEntries WHERE Tier = 1");
    }
}
