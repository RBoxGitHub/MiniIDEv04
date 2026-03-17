using MiniIDEv04.Data.Interfaces;
using MiniIDEv04.Models;
using SQLite;

namespace MiniIDEv04.Data.Sqlite
{
    public class SqliteSysControlRepository : ISysControlRepository
    {
        private readonly SQLiteAsyncConnection _db = ProjectDatabase.GetAsyncConnection();

        // ── IRepository<SysControl> ───────────────────────────────────────

        public Task<List<SysControl>> GetAllAsync()
            => _db.Table<SysControl>().OrderBy(c => c.ParentKey)
                                      .ThenBy(c => c.SortOrder)
                                      .ToListAsync();

        public async Task<SysControl?> GetByIdAsync(int id)
            => await _db.Table<SysControl>().Where(c => c.Id == id).FirstOrDefaultAsync();

        public async Task<int> InsertAsync(SysControl item)
        {
            item.CreatedAt = item.UpdatedAt = DateTime.UtcNow;
            await _db.InsertAsync(item);
            return item.Id;
        }

        public Task<int> UpdateAsync(SysControl item)
        {
            item.UpdatedAt = DateTime.UtcNow;
            return _db.UpdateAsync(item);
        }

        public Task<int> DeleteAsync(int id)
            => _db.ExecuteAsync("DELETE FROM sys_Controls WHERE Id=?", id);

        // ── ISysControlRepository ─────────────────────────────────────────

        public Task<List<SysControl>> GetByParentAsync(string parentKey)
            => _db.Table<SysControl>()
                  .Where(c => c.ParentKey == parentKey)
                  .OrderBy(c => c.SortOrder)
                  .ToListAsync();

        public async Task<SysControl?> GetByKeyAsync(string controlKey)
            => await _db.Table<SysControl>()
                        .Where(c => c.ControlKey == controlKey)
                        .FirstOrDefaultAsync();

        public Task<List<SysControlProperty>> GetPropertiesAsync(string controlKey)
            => _db.Table<SysControlProperty>()
                  .Where(p => p.ControlKey == controlKey)
                  .OrderBy(p => p.Category)
                  .ThenBy(p => p.SortOrder)
                  .ToListAsync();

        public async Task SavePropertyAsync(SysControlProperty prop)
        {
            prop.UpdatedAt = DateTime.UtcNow;
            var existing = await _db.Table<SysControlProperty>()
                .Where(p => p.ControlKey == prop.ControlKey
                         && p.PropertyName == prop.PropertyName)
                .FirstOrDefaultAsync();

            if (existing is null)
                await _db.InsertAsync(prop);
            else
            {
                prop.Id = existing.Id;
                await _db.UpdateAsync(prop);
            }
        }

        public Task DeletePropertiesAsync(string controlKey)
            => _db.ExecuteAsync(
                "DELETE FROM sys_ControlProperties WHERE ControlKey=?", controlKey);

        public async Task<int> CloneAsync(string sourceKey, string newKey, string newName)
        {
            var source = await GetByKeyAsync(sourceKey);
            if (source is null) return 0;

            var clone = new SysControl
            {
                ControlKey     = newKey,
                ControlName    = newName,
                ControlType    = source.ControlType,
                ParentKey      = source.ParentKey,
                ParentType     = source.ParentType,
                AssemblySource = source.AssemblySource,
                LayoutLeft     = (source.LayoutLeft ?? 0) + 20,
                LayoutTop      = (source.LayoutTop  ?? 0) + 20,
                LayoutRow      = source.LayoutRow,
                LayoutColumn   = source.LayoutColumn,
                Width          = source.Width,
                Height         = source.Height,
                IsVisible      = source.IsVisible,
                IsEnabled      = source.IsEnabled,
                SortOrder      = source.SortOrder + 1
            };

            var newId = await InsertAsync(clone);

            // Clone properties too
            var props = await GetPropertiesAsync(sourceKey);
            foreach (var p in props)
                await _db.InsertAsync(new SysControlProperty
                {
                    ControlKey    = newKey,
                    PropertyName  = p.PropertyName,
                    PropertyValue = p.PropertyValue,
                    PropertyType  = p.PropertyType,
                    Category      = p.Category,
                    SortOrder     = p.SortOrder
                });

            return newId;
        }
    }
}
