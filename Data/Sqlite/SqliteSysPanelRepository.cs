using MiniIDEv04.Data.Interfaces;
using MiniIDEv04.Models;
using SQLite;

namespace MiniIDEv04.Data.Sqlite
{
    public class SqliteSysPanelRepository : ISysPanelRepository
    {
        private readonly SQLiteAsyncConnection _db = ProjectDatabase.GetAsyncConnection();

        public Task<List<SysPanel>> GetAllAsync()
            => _db.Table<SysPanel>().OrderBy(p => p.SortOrder).ToListAsync();

        public async Task<SysPanel?> GetByIdAsync(int id)
            => await _db.Table<SysPanel>().Where(p => p.Id == id).FirstOrDefaultAsync();

        public async Task<SysPanel?> GetByKeyAsync(string panelKey)
            => await _db.Table<SysPanel>().Where(p => p.PanelKey == panelKey).FirstOrDefaultAsync();

        public Task<List<SysPanel>> GetVisibleAsync()
            => _db.Table<SysPanel>().Where(p => p.IsVisible).ToListAsync();

        public async Task<int> InsertAsync(SysPanel item)
        {
            item.CreatedAt = item.UpdatedAt = DateTime.UtcNow;
            await _db.InsertAsync(item);
            return item.Id;
        }

        public Task<int> UpdateAsync(SysPanel item)
        {
            item.UpdatedAt = DateTime.UtcNow;
            return _db.UpdateAsync(item);
        }

        public Task<int> DeleteAsync(int id)
            => _db.ExecuteAsync("DELETE FROM sys_Panels WHERE Id=?", id);

        public Task<int> SavePositionAsync(
            string panelKey, double left, double top, double width, double height)
            => _db.ExecuteAsync(
                @"UPDATE sys_Panels
                  SET PosLeft=?, PosTop=?, PanelWidth=?, PanelHeight=?, UpdatedAt=?
                  WHERE PanelKey=?",
                left, top, width, height, DateTime.UtcNow, panelKey);

        public Task<int> SetVisibilityAsync(string panelKey, bool visible)
            => _db.ExecuteAsync(
                "UPDATE sys_Panels SET IsVisible=?, UpdatedAt=? WHERE PanelKey=?",
                visible, DateTime.UtcNow, panelKey);

        public async Task<SysPanel> CloneAsync(string sourcePanelKey, string newName)
        {
            var source = await GetByKeyAsync(sourcePanelKey)
                ?? throw new InvalidOperationException($"Panel '{sourcePanelKey}' not found.");

            var clone = new SysPanel
            {
                PanelKey      = $"{sourcePanelKey}_Clone_{DateTime.UtcNow:yyyyMMddHHmmss}",
                PanelName     = newName,
                Description   = $"Cloned from {source.PanelName}",
                IsVisible     = true,
                IsPinned      = false,
                IsCloned      = true,
                ClonedFromKey = sourcePanelKey,
                PosLeft       = source.PosLeft  + 30,
                PosTop        = source.PosTop   + 30,
                PanelWidth    = source.PanelWidth,
                PanelHeight   = source.PanelHeight,
                TitleBarColor = source.TitleBarColor,
                Version       = source.Version,
                SortOrder     = source.SortOrder + 1
            };

            await InsertAsync(clone);
            return clone;
        }
    }
}
