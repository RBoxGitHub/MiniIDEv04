using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MiniIDEv04.Controls
{
    public partial class ucProjectZip : UserControl
    {
        // ── Folders to exclude ────────────────────────────────────────────────
        private static readonly string[] ExcludedFolders =
        {
            "bin", "obj", ".vs", ".git", "ProjectZipsStripped"
        };

        public ucProjectZip()
        {
            InitializeComponent();
        }

        // ── Button handler ────────────────────────────────────────────────────
        private async void ZipButton_Click(object sender, RoutedEventArgs e)
        {
            ZipButton.IsEnabled = false;
            ShowStatus("Finding project root…");

            try
            {
                // 1. Locate the project root
                string? projectRoot = FindProjectRoot();

                if (projectRoot is null)
                {
                    ShowStatus("❌  Could not locate project root (.csproj not found).");
                    return;
                }

                // 2. Build output path — <ProjectRoot>\ProjectZipsStripped\<FolderName>_yyyyMMdd_HHmm.zip
                string folderName  = new DirectoryInfo(projectRoot).Name;
                string timestamp   = DateTime.Now.ToString("yyyyMMdd_HHmm");
                string zipName     = $"{folderName}_{timestamp}.zip";
                string outputDir   = Path.Combine(projectRoot, "ProjectZipsStripped");
                Directory.CreateDirectory(outputDir);   // creates it if it doesn't exist yet
                string zipPath     = Path.Combine(outputDir, zipName);

                ShowStatus($"Zipping → {zipName} …");

                // 3. Collect files — exclude bin / obj / .vs / .git
                var allFiles = Directory.EnumerateFiles(projectRoot, "*.*",
                                    SearchOption.AllDirectories)
                    .Where(f => !IsExcluded(f, projectRoot))
                    .ToList();

                // 4. Write zip on background thread so UI stays responsive
                await System.Threading.Tasks.Task.Run(() =>
                {
                    if (File.Exists(zipPath)) File.Delete(zipPath);

                    using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);

                    foreach (var file in allFiles)
                    {
                        string entryName = Path.GetRelativePath(projectRoot, file)
                                               .Replace('\\', '/');
                        archive.CreateEntryFromFile(file, entryName,
                                                    CompressionLevel.Optimal);
                    }
                });

                ShowStatus($"✅  {allFiles.Count} files → {zipName}");
            }
            catch (Exception ex)
            {
                ShowStatus($"❌  {ex.Message}");
                MessageBox.Show($"Zip failed:\n\n{ex.Message}\n\n{ex.StackTrace}",
                                "Zip Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ZipButton.IsEnabled = true;
            }
        }

        // ── Walk up from BaseDirectory until a .csproj is found ──────────────
        private static string? FindProjectRoot()
        {
            string? dir = AppDomain.CurrentDomain.BaseDirectory;

            while (!string.IsNullOrEmpty(dir))
            {
                if (Directory.GetFiles(dir, "*.csproj").Length > 0)
                    return dir;

                dir = Path.GetDirectoryName(dir);
            }

            return null;
        }

        // ── True if the file lives under an excluded folder segment ───────────
        private static bool IsExcluded(string fullPath, string projectRoot)
        {
            string relative = Path.GetRelativePath(projectRoot, fullPath);

            // Skip previously-generated root-level zips so they don't nest
            if (Path.GetExtension(relative).Equals(".zip", StringComparison.OrdinalIgnoreCase)
                && !relative.Contains(Path.DirectorySeparatorChar))
                return true;

            var segments = relative.Split(Path.DirectorySeparatorChar,
                                          Path.AltDirectorySeparatorChar);

            return segments.Any(seg =>
                ExcludedFolders.Any(ex =>
                    seg.Equals(ex, StringComparison.OrdinalIgnoreCase)));
        }

        private void ShowStatus(string message)
        {
            StatusText.Text       = message;
            StatusText.Visibility = Visibility.Visible;
        }
    }
}
