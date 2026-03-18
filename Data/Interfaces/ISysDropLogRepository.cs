using MiniIDEv04.Models;

namespace MiniIDEv04.Data.Interfaces
{
    public interface ISysDropLogRepository : IRepository<SysDropLog>
    {
        Task<List<SysDropLog>> GetRecentAsync(int count = 100);
        Task<List<SysDropLog>> GetByZipAsync(string zipFileName);
        Task LogDropAsync(string zipFileName, string fileName,
                          string destination, string status);
    }
}
