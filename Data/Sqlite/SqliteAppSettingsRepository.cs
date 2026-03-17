using MiniIDEv04.Data.Interfaces;
using MiniIDEv04.Models;
using SQLite;

namespace MiniIDEv04.Data.Sqlite
{
    public class SqliteAppSettingsRepository : IAppSettingsRepository
    {
        private readonly SQLiteAsyncConnection _db = ProjectDatabase.GetAsyncConnection();

        public async Task<string?> GetValueAsync(string key)
        {
            var row = await _db.Table<SysAppSetting>()
                               .Where(s => s.Key == key)
                               .FirstOrDefaultAsync();
            return row?.Value;
        }

        public async Task<int> SetValueAsync(string key, string value, string? description = null)
        {
            var existing = await _db.Table<SysAppSetting>()
                                    .Where(s => s.Key == key)
                                    .FirstOrDefaultAsync();
            if (existing is null)
                return await _db.InsertAsync(new SysAppSetting
                {
                    Key         = key,
                    Value       = value,
                    Description = description,
                    UpdatedAt   = DateTime.UtcNow
                });

            existing.Value     = value;
            existing.UpdatedAt = DateTime.UtcNow;
            if (description is not null) existing.Description = description;
            return await _db.UpdateAsync(existing);
        }

        public Task<List<SysAppSetting>> GetAllAsync()
            => _db.Table<SysAppSetting>().OrderBy(s => s.Key).ToListAsync();
    }
}
