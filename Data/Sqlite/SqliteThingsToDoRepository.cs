using MiniIDEv04.Data.Interfaces;
using MiniIDEv04.Models;
using SQLite;

namespace MiniIDEv04.Data.Sqlite
{
    public class SqliteThingsToDoRepository : IThingsToDoRepository
    {
        private readonly SQLiteAsyncConnection _db = ProjectDatabase.GetAsyncConnection();

        public Task<List<ThingsToDo>> GetAllAsync()
            => _db.Table<ThingsToDo>()
                  .OrderBy(t => t.IsComplete)
                  .ThenBy(t => t.Priority)
                  .ThenByDescending(t => t.CreatedAt)
                  .ToListAsync();

        public async Task<ThingsToDo?> GetByIdAsync(int id)
            => await _db.Table<ThingsToDo>().Where(t => t.Id == id).FirstOrDefaultAsync();

        public Task<List<ThingsToDo>> GetOpenAsync()
            => _db.Table<ThingsToDo>().Where(t => !t.IsComplete)
                  .OrderBy(t => t.Priority).ToListAsync();

        public Task<List<ThingsToDo>> GetByPhaseAsync(int phase)
            => _db.Table<ThingsToDo>().Where(t => t.Phase == phase).ToListAsync();

        public async Task<int> InsertAsync(ThingsToDo item)
        {
            item.CreatedAt = item.UpdatedAt = DateTime.UtcNow;
            await _db.InsertAsync(item);
            return item.Id;
        }

        public Task<int> UpdateAsync(ThingsToDo item)
        {
            item.UpdatedAt = DateTime.UtcNow;
            return _db.UpdateAsync(item);
        }

        public Task<int> DeleteAsync(int id)
            => _db.ExecuteAsync("DELETE FROM sys_ThingsToDo WHERE Id=?", id);

        public Task<int> MarkCompleteAsync(int id)
            => _db.ExecuteAsync(
                "UPDATE sys_ThingsToDo SET IsComplete=1, UpdatedAt=? WHERE Id=?",
                DateTime.UtcNow, id);

        public Task<int> QuickAddAsync(
            string note, int phase = 1, int priority = 2,
            string? refObject = null, string? typeRef = null,
            string? fileName = null, string? filePath = null)
            => InsertAsync(new ThingsToDo
            {
                SideNote        = note,
                Phase           = phase,
                Priority        = priority,
                ReferenceObject = refObject,
                TypeReference   = typeRef,
                FileName        = fileName,
                FilePath        = filePath
            });
    }
}
