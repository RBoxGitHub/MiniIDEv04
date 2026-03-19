using MiniIDEv04.Models;

namespace MiniIDEv04.Data.Interfaces
{
    /// <summary>
    /// Repository interface for DLL watcher — registers new Tier 2 libraries
    /// and checks if a library is already known.
    /// </summary>
    public interface IDllWatcherRepository
    {
        /// <summary>
        /// Returns the library record for the given assembly path if it exists.
        /// Returns null if not yet registered.
        /// </summary>
        Task<SysLibrary?> GetByPathAsync(string assemblyPath);

        /// <summary>
        /// Registers a new Tier 2 DLL library in sys_Libraries.
        /// Returns the inserted record with its new Id.
        /// </summary>
        Task<SysLibrary> RegisterAsync(string assemblyPath);

        /// <summary>Returns all Tier 2 libraries (DLL drop-ins).</summary>
        Task<List<SysLibrary>> GetTier2LibrariesAsync();
    }
}
