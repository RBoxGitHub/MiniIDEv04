using MiniIDEv04.Data.Interfaces;
using MiniIDEv04.Models;
using SQLite;
using System.IO;
using System.Reflection;

namespace MiniIDEv04.Data.Sqlite
{
    public class SqliteDllWatcherRepository : IDllWatcherRepository
    {
        private readonly SQLiteAsyncConnection _db = ProjectDatabase.GetAsyncConnection();

        public async Task<SysLibrary?> GetByPathAsync(string assemblyPath)
            => await _db.Table<SysLibrary>()
                        .Where(l => l.AssemblyPath == assemblyPath)
                        .FirstOrDefaultAsync();

        public async Task<SysLibrary> RegisterAsync(string assemblyPath)
        {
            // Try to get assembly name from the DLL itself
            string assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
            string version      = "unknown";

            try
            {
                var name = AssemblyName.GetAssemblyName(assemblyPath);
                assemblyName = name.Name ?? assemblyName;
                version      = name.Version?.ToString() ?? version;
            }
            catch { /* use filename fallback */ }

            var lib = new SysLibrary
            {
                LibraryKey    = $"DLL_{assemblyName}_{DateTime.UtcNow:yyyyMMddHHmmss}",
                LibraryName   = assemblyName,
                AssemblyName  = assemblyName,
                AssemblyPath  = assemblyPath,
                Version       = version,
                Tier          = 2,
                IsScanEnabled = true,
                IsLoaded      = false,
                IsVisible     = true,
                CreatedAt     = DateTime.UtcNow,
                UpdatedAt     = DateTime.UtcNow,
            };

            await _db.InsertAsync(lib);
            return lib;
        }

        public Task<List<SysLibrary>> GetTier2LibrariesAsync()
            => _db.Table<SysLibrary>()
                  .Where(l => l.Tier == 2 && l.IsVisible)
                  .ToListAsync();
    }
}
