using MiniIDEv04.Models;

namespace MiniIDEv04.Data.Interfaces
{
    public interface IAppSettingsRepository
    {
        Task<string?>             GetValueAsync(string key);
        Task<int>                 SetValueAsync(string key, string value, string? description = null);
        Task<List<SysAppSetting>> GetAllAsync();
    }
}
