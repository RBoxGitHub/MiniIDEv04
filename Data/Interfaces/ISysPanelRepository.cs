using MiniIDEv04.Models;

namespace MiniIDEv04.Data.Interfaces
{
    public interface ISysPanelRepository : IRepository<SysPanel>
    {
        Task<SysPanel?>      GetByKeyAsync(string panelKey);
        Task<List<SysPanel>> GetVisibleAsync();
        Task<int>            SavePositionAsync(string panelKey,
                                 double left, double top,
                                 double width, double height);
        Task<int>            SetVisibilityAsync(string panelKey, bool visible);
        Task<SysPanel>       CloneAsync(string sourcePanelKey, string newName);
    }
}
