using MiniIDEv04.Models;

namespace MiniIDEv04.Data.Interfaces
{
    public interface ISysGitLogRepository : IRepository<SysGitLog>
    {
        Task<List<SysGitLog>> GetRecentAsync(int count = 50);
        Task LogPushAsync(string commitMessage, string versionTag,
                          bool success, string gitOutput);
    }
}
