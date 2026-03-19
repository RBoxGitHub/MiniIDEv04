using MiniIDEv04.Data.Interfaces;
using MiniIDEv04.Data.Sqlite;
using MiniIDEv04.Models;
using MiniIDEv04.Services;
using System.IO;
using System.Text;

namespace MiniIDEv04.Services
{
    /// <summary>
    /// Phase 2 Item 7 — Tier 2 DLL drop-in folder watcher.
    ///
    /// Watches a configured folder for new .dll files.
    /// When a DLL lands:
    ///   1. Registers it in sys_Libraries (Tier 2)
    ///   2. Runs LibraryScanner against it
    ///   3. Reloads ToolboxRegistry so new controls appear immediately
    ///   4. Optionally writes a scan log file
    ///   5. Fires DllScanned event so UI can show a notification
    ///
    /// Uses a debounce timer to prevent double-fires from file copy operations.
    /// </summary>
    public class DllWatcherService : IDisposable
    {
        private readonly IDllWatcherRepository  _repo;
        private readonly LibraryScanner         _scanner;
        private readonly ToolboxRegistry        _registry;
        private FileSystemWatcher?              _watcher;
        private readonly Dictionary<string, Timer> _debounceTimers = new();
        private readonly object                 _lock = new();

        private const int DebounceMs = 1500; // wait 1.5s after last file event

        public string WatchFolder { get; private set; } = string.Empty;
        public bool   IsWatching  => _watcher?.EnableRaisingEvents == true;

        /// <summary>
        /// External progress handler injected by the VM.
        /// Auto-watch scans report through this so ToolboxWindow progress bar
        /// updates even for background DLL drops.
        /// </summary>
        public IProgress<LibraryScanProgress>? ExternalProgress { get; set; }

        /// <summary>Fired when a DLL has been scanned successfully.</summary>
        public event EventHandler<DllScannedEventArgs>? DllScanned;

        public DllWatcherService(
            ToolboxRegistry        registry,
            IDllWatcherRepository? repo    = null,
            LibraryScanner?        scanner = null)
        {
            _registry = registry;
            _repo     = repo    ?? new SqliteDllWatcherRepository();
            _scanner  = scanner ?? new LibraryScanner();
        }

        // ── Start / Stop ──────────────────────────────────────────────

        public void Start(string watchFolder)
        {
            Stop();

            WatchFolder = watchFolder;

            if (!Directory.Exists(watchFolder))
                Directory.CreateDirectory(watchFolder);

            _watcher = new FileSystemWatcher(watchFolder, "*.dll")
            {
                NotifyFilter          = NotifyFilters.FileName | NotifyFilters.LastWrite,
                EnableRaisingEvents   = true,
                IncludeSubdirectories = false,
            };

            _watcher.Created += OnFileEvent;
            _watcher.Changed += OnFileEvent;
        }

        public void Stop()
        {
            if (_watcher is not null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Created -= OnFileEvent;
                _watcher.Changed -= OnFileEvent;
                _watcher.Dispose();
                _watcher = null;
            }

            lock (_lock)
            {
                foreach (var t in _debounceTimers.Values) t.Dispose();
                _debounceTimers.Clear();
            }
        }

        // ── File event with debounce ───────────────────────────────────

        private void OnFileEvent(object sender, FileSystemEventArgs e)
        {
            var path = e.FullPath;

            lock (_lock)
            {
                // Cancel existing debounce for this path
                if (_debounceTimers.TryGetValue(path, out var existing))
                {
                    existing.Dispose();
                    _debounceTimers.Remove(path);
                }

                // Start new debounce timer
                var timer = new Timer(_ => ProcessDllAsync(path).ConfigureAwait(false),
                    null, DebounceMs, Timeout.Infinite);
                _debounceTimers[path] = timer;
            }
        }

        // ── Process a newly dropped DLL ───────────────────────────────

        public async Task ProcessDllAsync(
            string dllPath,
            IProgress<LibraryScanProgress>? progress = null,
            bool saveLog = false)
        {
            // Use injected external progress if none explicitly supplied
            progress ??= ExternalProgress;
            lock (_lock)
            {
                if (_debounceTimers.ContainsKey(dllPath))
                {
                    _debounceTimers[dllPath].Dispose();
                    _debounceTimers.Remove(dllPath);
                }
            }

            if (!File.Exists(dllPath)) return;

            var dllName    = Path.GetFileName(dllPath);
            var logLines   = new List<string>();
            var startTime  = DateTime.Now;

            void Report(string msg, int pct)
            {
                progress?.Report(new LibraryScanProgress(msg, pct));
                logLines.Add($"  {msg,-60} {pct,3}%");
            }

            try
            {
                Report($"🔍 New DLL detected: {dllName}", 5);

                // Check if already registered
                var existing = await _repo.GetByPathAsync(dllPath);
                SysLibrary lib;

                if (existing is not null)
                {
                    lib = existing;
                    Report($"📋 Already registered: {lib.LibraryName} — re-scanning...", 15);
                }
                else
                {
                    Report($"📝 Registering {dllName} in sys_Libraries...", 15);
                    lib = await _repo.RegisterAsync(dllPath);
                    Report($"✅ Registered as: {lib.LibraryName} v{lib.Version}", 25);
                }

                // Scan the library
                Report($"🔬 Scanning {lib.LibraryName} for controls...", 30);

                var scanProgress = new Progress<LibraryScanProgress>(p =>
                {
                    progress?.Report(p);
                    logLines.Add($"  {p.Message,-60} {(p.ProgressPercent >= 0 ? p.ProgressPercent + "%" : "")}");
                });

                var result = await _scanner.ScanAllAsync(scanProgress);

                Report($"✅ Scan complete — {result.EntriesAdded} new controls found", 90);

                // Reload toolbox registry
                Report("🔄 Reloading toolbox...", 95);
                await _registry.ReloadAsync();

                Report($"🎉 Done — toolbox updated with controls from {dllName}", 100);

                // Save log if requested
                if (saveLog)
                    SaveScanLog(dllName, dllPath, logLines, result, startTime);

                // Fire event for UI notification
                DllScanned?.Invoke(this, new DllScannedEventArgs(
                    dllName:       dllName,
                    libraryName:   lib.LibraryName,
                    entriesAdded:  result.EntriesAdded,
                    hasErrors:     result.HasErrors,
                    errors:        result.Errors));
            }
            catch (Exception ex)
            {
                progress?.Report(new LibraryScanProgress(
                    $"❌ Failed to process {dllName}: {ex.Message}", -1));
            }
        }

        // ── Log file writer ───────────────────────────────────────────

        private void SaveScanLog(
            string dllName,
            string dllPath,
            List<string> logLines,
            LibraryScanResult result,
            DateTime startTime)
        {
            try
            {
                var timestamp = startTime.ToString("yyyyMMdd_HHmmss");
                var logName   = $"{Path.GetFileNameWithoutExtension(dllName)}_{timestamp}_scan.log";
                var logPath   = Path.Combine(WatchFolder, logName);
                var separator = new string('─', 60);

                var sb = new StringBuilder();
                sb.AppendLine("MiniIDEv04 DLL Scan Log");
                sb.AppendLine(separator);
                sb.AppendLine($"DLL:     {dllName}");
                sb.AppendLine($"Path:    {dllPath}");
                sb.AppendLine($"Date:    {startTime:MMM dd yyyy  HH:mm:ss}");
                sb.AppendLine(separator);
                sb.AppendLine();
                sb.AppendLine("PROCESS NARRATIVE");
                sb.AppendLine(separator);
                foreach (var line in logLines)
                    sb.AppendLine(line);
                sb.AppendLine();
                sb.AppendLine("SUMMARY");
                sb.AppendLine(separator);
                sb.AppendLine($"Controls added:    {result.EntriesAdded}");
                sb.AppendLine($"Libraries scanned: {result.LibrariesScanned}");
                sb.AppendLine($"Groups added:      {result.GroupsAdded}");

                if (result.HasErrors)
                {
                    sb.AppendLine();
                    sb.AppendLine("ERRORS");
                    sb.AppendLine(separator);
                    foreach (var err in result.Errors)
                        sb.AppendLine($"  ❌ {err}");
                }

                sb.AppendLine(separator);
                sb.AppendLine($"Duration: {(DateTime.Now - startTime).TotalSeconds:F1}s");

                File.WriteAllText(logPath, sb.ToString(), Encoding.UTF8);
            }
            catch { /* log write failure is non-fatal */ }
        }

        public void Dispose() => Stop();
    }

    // ── Event args ────────────────────────────────────────────────────

    public class DllScannedEventArgs : EventArgs
    {
        public string       DllName      { get; }
        public string       LibraryName  { get; }
        public int          EntriesAdded { get; }
        public bool         HasErrors    { get; }
        public List<string> Errors       { get; }

        public DllScannedEventArgs(
            string dllName, string libraryName,
            int entriesAdded, bool hasErrors, List<string> errors)
        {
            DllName      = dllName;
            LibraryName  = libraryName;
            EntriesAdded = entriesAdded;
            HasErrors    = hasErrors;
            Errors       = errors;
        }
    }
}
