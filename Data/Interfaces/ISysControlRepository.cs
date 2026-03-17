using MiniIDEv04.Models;

namespace MiniIDEv04.Data.Interfaces
{
    public interface ISysControlRepository : IRepository<SysControl>
    {
        Task<List<SysControl>> GetByParentAsync(string parentKey);
        Task<SysControl?> GetByKeyAsync(string controlKey);
        Task<List<SysControlProperty>> GetPropertiesAsync(string controlKey);
        Task SavePropertyAsync(SysControlProperty prop);
        Task DeletePropertiesAsync(string controlKey);
        Task<int> CloneAsync(string sourceKey, string newKey, string newName);
    }
}
