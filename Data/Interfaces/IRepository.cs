namespace MiniIDEv04.Data.Interfaces
{
    public interface IRepository<T> where T : class, new()
    {
        Task<List<T>> GetAllAsync();
        Task<T?>      GetByIdAsync(int id);
        Task<int>     InsertAsync(T item);
        Task<int>     UpdateAsync(T item);
        Task<int>     DeleteAsync(int id);
    }
}
