using MiniIDEv04.Data.Interfaces;
using MiniIDEv04.Models;
using SQLite;

namespace MiniIDEv04.Data.Sqlite
{
    public class SqliteDropLogRepository : ISysDropLogRepository
    {
        private readonly SQLiteAsyncConnection _db = ProjectDatabase.GetAsyncConnection();

        public Task<List<SysDropLog>> GetAllAsync()
            => _db.Table<SysDropLog>()
                  .OrderByDescending(d => d.DroppedAt)
                  .ToListAsync();

        public Task<List<SysDropLog>> GetRecentAsync(int count = 100)
            => _db.Table<SysDropLog>()
                  .OrderByDescending(d => d.DroppedAt)
                  .Take(count)
                  .ToListAsync();

        public Task<List<SysDropLog>> GetByZipAsync(string zipFileName)
            => _db.Table<SysDropLog>()
                  .Where(d => d.ZipFileName == zipFileName)
                  .OrderBy(d => d.FileName)
                  .ToListAsync();

        public async Task<SysDropLog?> GetByIdAsync(int id)
            => await _db.Table<SysDropLog>().Where(d => d.Id == id).FirstOrDefaultAsync();

        public async Task<int> InsertAsync(SysDropLog item)
        {
            item.DroppedAt = DateTime.UtcNow;
            await _db.InsertAsync(item);
            return item.Id;
        }

        public Task<int> UpdateAsync(SysDropLog item)
            => _db.UpdateAsync(item);

        public Task<int> DeleteAsync(int id)
            => _db.ExecuteAsync("DELETE FROM sys_DropLog WHERE Id=?", id);

        public Task LogDropAsync(string zipFileName, string fileName,
                                 string destination, string status)
            => InsertAsync(new SysDropLog
            {
                ZipFileName = zipFileName,
                FileName    = fileName,
                Destination = destination,
                Status      = status,
                DroppedAt   = DateTime.UtcNow
            });
    }
}
