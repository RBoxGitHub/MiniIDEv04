using MiniIDEv04.Models;
using System.IO;
using System.IO.Compression;
using System.Text.Json;

namespace MiniIDEv04.Services
{
    /// <summary>
    /// Reads a Claude-delivered zip file, parses _manifest.json,
    /// copies each file to the correct project folder, and returns
    /// a list of DropResult rows for display and DB logging.
    /// Supports IProgress&lt;ZipDropProgress&gt; for live UI feedback.
    /// </summary>
    public class ZipDropService
    {
        public record DropResult(
            string ZipFileName,
            string FileName,
            string Destination,
            string FullDestPath,
            string Status   // "New" | "Updated" | "Skipped" | "Error"
        );

        public record ZipDropProgress(
            string Message,
            int    ProgressPercent  // 0–100; -1 = indeterminate
        );

        private record ManifestFile(string source, string target, string action, string description);
        private record Manifest(string? commit_message, List<ManifestFile>? files);

        // ── Public entry point ────────────────────────────────────────

        public List<DropResult> ProcessZip(
            string zipPath,
            string projectRoot,
            IProgress<ZipDropProgress>? progress = null)
        {
            var results = new List<DropResult>();
            var zipName = Path.GetFileName(zipPath);

            var tempDir = Path.Combine(
                Path.GetTempPath(),
                $"MiniIDEv04_Drop_{Guid.NewGuid().ToString("N")[..8]}");

            try
            {
                // ── Stage 1: Extract ──────────────────────────────────
                progress?.Report(new ZipDropProgress("📦 Extracting zip...", 10));
                Directory.CreateDirectory(tempDir);
                ZipFile.ExtractToDirectory(zipPath, tempDir, overwriteFiles: true);

                // ── Stage 2: Read manifest ────────────────────────────
                progress?.Report(new ZipDropProgress("📋 Reading manifest...", 25));

                var manifestPath = Path.Combine(tempDir, "_manifest.json");
                if (!File.Exists(manifestPath))
                {
                    progress?.Report(new ZipDropProgress(
                        "⚠ No manifest found — using folder structure...", 30));
                    results.AddRange(ProcessWithoutManifest(
                        tempDir, projectRoot, zipName, progress));
                    return results;
                }

                var json     = File.ReadAllText(manifestPath);
                var manifest = JsonSerializer.Deserialize<Manifest>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var entries = manifest?.files ?? new List<ManifestFile>();

                if (entries.Count == 0)
                {
                    progress?.Report(new ZipDropProgress(
                        "⚠ Manifest has no files — using folder structure...", 30));
                    results.AddRange(ProcessWithoutManifest(
                        tempDir, projectRoot, zipName, progress));
                    return results;
                }

                // ── Stage 3: Copy files ───────────────────────────────
                int total   = entries.Count;
                int current = 0;

                foreach (var entry in entries)
                {
                    current++;

                    // Progress: 25% → 90% scaled across all files
                    int pct = 25 + (int)((double)current / total * 65);
                    var shortName = Path.GetFileName(entry.source);

                    progress?.Report(new ZipDropProgress(
                        $"📄 Copying file {current} of {total} — {shortName}", pct));

                    var normalizedSrc = entry.source.Replace('/', Path.DirectorySeparatorChar);
                    var srcFile       = Path.Combine(tempDir, normalizedSrc);

                    if (!File.Exists(srcFile))
                    {
                        results.Add(new DropResult(zipName, shortName,
                            Path.GetDirectoryName(entry.target) ?? ".",
                            "", "Skipped — not found in zip"));
                        continue;
                    }

                    var targetRelDir = Path.GetDirectoryName(
                        entry.target.Replace('/', Path.DirectorySeparatorChar)) ?? "";

                    var destDir = string.IsNullOrWhiteSpace(targetRelDir)
                        ? projectRoot
                        : Path.Combine(projectRoot, targetRelDir);

                    Directory.CreateDirectory(destDir);

                    var destFile = Path.Combine(destDir, Path.GetFileName(entry.source));
                    var status   = File.Exists(destFile) ? "Updated" : "New";

                    try
                    {
                        File.Copy(srcFile, destFile, overwrite: true);
                        results.Add(new DropResult(zipName, shortName,
                            targetRelDir, destFile, status));
                    }
                    catch (Exception ex)
                    {
                        results.Add(new DropResult(zipName, shortName,
                            targetRelDir, destFile, $"Error: {ex.Message}"));
                    }
                }

                // ── Stage 4: Done ─────────────────────────────────────
                progress?.Report(new ZipDropProgress("🗄 Logging results...", 95));
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch { }
            }

            return results;
        }

        // ── Fallback: no manifest ─────────────────────────────────────

        private static List<DropResult> ProcessWithoutManifest(
            string tempDir, string projectRoot, string zipName,
            IProgress<ZipDropProgress>? progress)
        {
            var results = new List<DropResult>();
            var files   = Directory.GetFiles(tempDir, "*.*", SearchOption.AllDirectories)
                .Where(f => Path.GetFileName(f) != "_manifest.json" &&
                            !Path.GetFileName(f).EndsWith("DropChecklist.txt"))
                .ToList();

            int total   = files.Count;
            int current = 0;

            foreach (var srcFile in files)
            {
                current++;
                int pct       = 30 + (int)((double)current / total * 60);
                var shortName = Path.GetFileName(srcFile);

                progress?.Report(new ZipDropProgress(
                    $"📄 Copying file {current} of {total} — {shortName}", pct));

                var relative = Path.GetRelativePath(tempDir, srcFile);
                var destFile = Path.Combine(projectRoot, relative);
                var destDir  = Path.GetDirectoryName(destFile)!;
                var status   = File.Exists(destFile) ? "Updated" : "New";

                try
                {
                    Directory.CreateDirectory(destDir);
                    File.Copy(srcFile, destFile, overwrite: true);
                    results.Add(new DropResult(zipName, shortName,
                        Path.GetDirectoryName(relative) ?? ".", destFile, status));
                }
                catch (Exception ex)
                {
                    results.Add(new DropResult(zipName, shortName,
                        Path.GetDirectoryName(relative) ?? ".", destFile,
                        $"Error: {ex.Message}"));
                }
            }

            return results;
        }

        // ── Commit message extraction (used by DropZoneWindow) ────────

        public static string ExtractCommitMessage(string zipPath)
        {
            var tempDir = Path.Combine(
                Path.GetTempPath(),
                $"MiniIDEv04_Peek_{Guid.NewGuid().ToString("N")[..8]}");

            try
            {
                Directory.CreateDirectory(tempDir);
                ZipFile.ExtractToDirectory(zipPath, tempDir, overwriteFiles: true);

                var manifestPath = Path.Combine(tempDir, "_manifest.json");
                if (!File.Exists(manifestPath)) return string.Empty;

                var json = File.ReadAllText(manifestPath);
                using var doc  = JsonDocument.Parse(json);
                var root = doc.RootElement;

                return root.TryGetProperty("commit_message", out var cm)
                    ? cm.GetString() ?? string.Empty
                    : string.Empty;
            }
            catch { return string.Empty; }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch { }
            }
        }
    }
}
