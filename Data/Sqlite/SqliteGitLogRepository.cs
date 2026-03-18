using MiniIDEv04.Data.Interfaces;
using MiniIDEv04.Models;
using SQLite;

namespace MiniIDEv04.Data.Sqlite
{
    public class SqliteGitLogRepository : ISysGitLogRepository
    {
        private readonly SQLiteAsyncConnection _db = ProjectDatabase.GetAsyncConnection();

        public Task<List<SysGitLog>> GetAllAsync()
            => _db.Table<SysGitLog>()
                  .OrderByDescending(g => g.PushedAt)
                  .ToListAsync();

        public Task<List<SysGitLog>> GetRecentAsync(int count = 50)
            => _db.Table<SysGitLog>()
                  .OrderByDescending(g => g.PushedAt)
                  .Take(count)
                  .ToListAsync();

        public async Task<SysGitLog?> GetByIdAsync(int id)
            => await _db.Table<SysGitLog>().Where(g => g.Id == id).FirstOrDefaultAsync();

        public async Task<int> InsertAsync(SysGitLog item)
        {
            item.PushedAt = DateTime.UtcNow;
            await _db.InsertAsync(item);
            return item.Id;
        }

        public Task<int> UpdateAsync(SysGitLog item)
            => _db.UpdateAsync(item);

        public Task<int> DeleteAsync(int id)
            => _db.ExecuteAsync("DELETE FROM sys_GitLog WHERE Id=?", id);

        public Task LogPushAsync(string commitMessage, string versionTag,
                                 bool success, string gitOutput)
            => InsertAsync(new SysGitLog
            {
                CommitMessage = commitMessage,
                VersionTag    = versionTag,
                Success       = success,
                GitOutput     = gitOutput,
                PushedAt      = DateTime.UtcNow
            });
    }
}
