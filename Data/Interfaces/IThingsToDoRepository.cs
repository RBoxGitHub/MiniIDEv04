using MiniIDEv04.Models;

namespace MiniIDEv04.Data.Interfaces
{
    public interface IThingsToDoRepository : IRepository<ThingsToDo>
    {
        Task<List<ThingsToDo>> GetOpenAsync();
        Task<List<ThingsToDo>> GetByPhaseAsync(int phase);
        Task<int>              MarkCompleteAsync(int id);
        Task<int>              QuickAddAsync(
            string  note,
            int     phase        = 1,
            int     priority     = 2,
            string? refObject    = null,
            string? typeRef      = null,
            string? fileName     = null,
            string? filePath     = null);
    }
}
