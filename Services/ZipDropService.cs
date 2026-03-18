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

        private record ManifestEntry(string file, string destination);

        /// <summary>
        /// Process a zip file against the project root.
        /// Returns one DropResult per file in the manifest.
        /// </summary>
        public List<DropResult> ProcessZip(string zipPath, string projectRoot)
        {
            var results   = new List<DropResult>();
            var zipName   = Path.GetFileName(zipPath);
            var tempDir   = Path.Combine(Path.GetTempPath(), $"MiniIDEv04_Drop_{Guid.NewGuid():N[..8]}");

            try
            {
                // Extract to temp
                Directory.CreateDirectory(tempDir);
                ZipFile.ExtractToDirectory(zipPath, tempDir, overwriteFiles: true);

                // Read manifest
                var manifestPath = Path.Combine(tempDir, "_manifest.json");
                if (!File.Exists(manifestPath))
                {
                    // No manifest — try to process all files using folder structure
                    results.AddRange(ProcessWithoutManifest(tempDir, projectRoot, zipName));
                    return results;
                }

                var json    = File.ReadAllText(manifestPath);
                var entries = JsonSerializer.Deserialize<List<ManifestEntry>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (entries is null || entries.Count == 0)
                    return results;

                foreach (var entry in entries)
                {
                    var srcFile  = Path.Combine(tempDir, entry.file);
                    if (!File.Exists(srcFile))
                    {
                        results.Add(new DropResult(zipName, entry.file,
                            entry.destination, "", "Skipped — not found in zip"));
                        continue;
                    }

                    var destDir  = entry.destination == "."
                        ? projectRoot
                        : Path.Combine(projectRoot, entry.destination.TrimEnd('\\'));

                    Directory.CreateDirectory(destDir);

                    var destFile = Path.Combine(destDir, Path.GetFileName(entry.file));
                    var status   = File.Exists(destFile) ? "Updated" : "New";

                    try
                    {
                        File.Copy(srcFile, destFile, overwrite: true);
                        results.Add(new DropResult(zipName, Path.GetFileName(entry.file),
                            entry.destination, destFile, status));
                    }
                    catch (Exception ex)
                    {
                        results.Add(new DropResult(zipName, Path.GetFileName(entry.file),
                            entry.destination, destFile, $"Error: {ex.Message}"));
                    }
                }
            }
            finally
            {
                // Clean up temp
                try { Directory.Delete(tempDir, recursive: true); } catch { }
            }

            return results;
        }

        /// <summary>
        /// Fallback: no manifest — use folder structure inside zip as destination map.
        /// e.g. Controls\SomeFile.cs → ProjectRoot\Controls\SomeFile.cs
        /// </summary>
        private List<DropResult> ProcessWithoutManifest(
            string tempDir, string projectRoot, string zipName)
        {
            var results = new List<DropResult>();
            var files   = Directory.GetFiles(tempDir, "*.*", SearchOption.AllDirectories);

            foreach (var srcFile in files)
            {
                var relative = Path.GetRelativePath(tempDir, srcFile);
                var destFile = Path.Combine(projectRoot, relative);
                var destDir  = Path.GetDirectoryName(destFile)!;
                var status   = File.Exists(destFile) ? "Updated" : "New";

                try
                {
                    Directory.CreateDirectory(destDir);
                    File.Copy(srcFile, destFile, overwrite: true);
                    results.Add(new DropResult(zipName,
                        Path.GetFileName(srcFile),
                        Path.GetDirectoryName(relative) ?? ".",
                        destFile, status));
                }
                catch (Exception ex)
                {
                    results.Add(new DropResult(zipName,
                        Path.GetFileName(srcFile),
                        Path.GetDirectoryName(relative) ?? ".",
                        destFile, $"Error: {ex.Message}"));
                }
            }

            return results;
        }
    }
}
