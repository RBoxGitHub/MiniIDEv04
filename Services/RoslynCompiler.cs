using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MiniIDEv04.Data.Interfaces;
using MiniIDEv04.Models;

namespace MiniIDEv04.Services
{
    /// <summary>
    /// Compiles C# source code entered in the CodeEditor pane using Roslyn.
    /// Assembly references are loaded dynamically from sys_Libraries plus a
    /// fixed set of standard WPF / runtime assemblies.
    /// Never throws to the caller — all errors are returned in RoslynCompileResult.
    /// </summary>
    public class RoslynCompiler
    {
        // ── Dependencies ────────────────────────────────────────────────────

        private readonly ILibraryScannerRepository _libraryRepo;

        // ── Constructor ─────────────────────────────────────────────────────

        public RoslynCompiler(ILibraryScannerRepository libraryRepo)
        {
            _libraryRepo = libraryRepo;
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Compiles <paramref name="csharpSource"/> and returns a RoslynCompileResult.
        /// On success, result.Assembly is the in-memory loaded assembly.
        /// On failure, result.Diagnostics contains the error list.
        /// </summary>
        public async Task<RoslynCompileResult> CompileAsync(string csharpSource)
        {
            var diagnostics = new List<string>();

            try
            {
                // 1. Build the syntax tree
                var syntaxTree = CSharpSyntaxTree.ParseText(csharpSource);

                // 2. Gather all MetadataReferences
                var references = await BuildReferencesAsync(diagnostics);
                if (references.Count == 0)
                {
                    diagnostics.Insert(0, "No valid assembly references could be resolved. Compilation aborted.");
                    return RoslynCompileResult.Fail(diagnostics);
                }

                // 3. Configure the compilation
                var assemblyName = $"MiniIDEDynamic_{Guid.NewGuid():N}";
                var options = new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Debug,
                    allowUnsafe: false);

                var compilation = CSharpCompilation.Create(
                    assemblyName,
                    syntaxTrees: [syntaxTree],
                    references: references,
                    options: options);

                // 4. Emit to memory
                using var ms = new MemoryStream();
                var emitResult = compilation.Emit(ms);

                // 5. Collect diagnostics (errors + warnings)
                var roslynDiags = emitResult.Diagnostics
                    .Where(d => d.Severity >= DiagnosticSeverity.Warning)
                    .Select(d => $"[{d.Severity}] {d.Id}: {d.GetMessage()} " +
                                 $"(Line {d.Location.GetLineSpan().StartLinePosition.Line + 1})")
                    .ToList();

                diagnostics.AddRange(roslynDiags);

                if (!emitResult.Success)
                    return RoslynCompileResult.Fail(diagnostics);

                // 6. Load the assembly from the memory stream
                ms.Seek(0, SeekOrigin.Begin);
                var assembly = Assembly.Load(ms.ToArray());

                return RoslynCompileResult.Ok(assembly, diagnostics);
            }
            catch (Exception ex)
            {
                diagnostics.Insert(0, $"[Fatal] RoslynCompiler threw an unexpected exception: {ex.Message}");
                return RoslynCompileResult.Fail(diagnostics);
            }
        }

        // ── Private Helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Builds the full set of MetadataReferences:
        ///   - Standard runtime / WPF assemblies (always included)
        ///   - Dynamic entries from sys_Libraries (DLL paths stored in DB)
        /// Errors loading individual references are logged to <paramref name="diagnostics"/>
        /// but do not abort the process — we continue with whatever resolved.
        /// </summary>
        private async Task<List<MetadataReference>> BuildReferencesAsync(List<string> diagnostics)
        {
            var refs = new List<MetadataReference>();

            // ── Standard implicit references ────────────────────────────────
            // These mirror what a default WPF project would reference.
            var implicitAssemblies = new[]
            {
                typeof(object).Assembly,                                    // System.Private.CoreLib
                typeof(Console).Assembly,                                   // System.Console
                Assembly.Load("System.Runtime"),                            // System.Runtime
                Assembly.Load("System.Collections"),                        // System.Collections
                Assembly.Load("System.Linq"),                               // System.Linq
                Assembly.Load("System.ComponentModel"),                     // System.ComponentModel
                Assembly.Load("netstandard"),                               // netstandard shim
                typeof(System.Windows.UIElement).Assembly,                  // PresentationCore
                typeof(System.Windows.Window).Assembly,                     // PresentationFramework
                typeof(System.Windows.Media.Brush).Assembly,               // PresentationCore (already above, deduped)
                typeof(System.Xaml.XamlReader).Assembly,                   // System.Xaml
                Assembly.Load("WindowsBase"),                               // WindowsBase
            };

            foreach (var asm in implicitAssemblies)
            {
                TryAddAssemblyRef(asm, refs, diagnostics);
            }

            // Also add the reference assembly facades via AppContext
            // (needed for .NET 6+ to resolve framework types properly)
            var runtimeDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
            foreach (var facade in new[] { "System.dll", "mscorlib.dll" })
            {
                var facadePath = Path.Combine(runtimeDir, facade);
                if (File.Exists(facadePath))
                    TryAddPathRef(facadePath, refs, diagnostics);
            }

            // ── Dynamic references from sys_Libraries ───────────────────────
            try
            {
                // GetScanEnabledAsync returns libraries where IsScanEnabled = true.
                // We further filter by IsLoaded — the flag the model marks as
                // "RoslynCompiler uses this to build dynamic assembly refs."
                var libraries = await _libraryRepo.GetScanEnabledAsync();
                foreach (var lib in libraries.Where(l => l.IsLoaded))
                {
                    if (string.IsNullOrWhiteSpace(lib.AssemblyPath))
                        continue;

                    if (!File.Exists(lib.AssemblyPath))
                    {
                        diagnostics.Add($"[Warning] Library '{lib.LibraryName}' not found at: {lib.AssemblyPath}");
                        continue;
                    }

                    TryAddPathRef(lib.AssemblyPath, refs, diagnostics);
                }
            }
            catch (Exception ex)
            {
                diagnostics.Add($"[Warning] Could not load sys_Libraries: {ex.Message}");
            }

            // Deduplicate by display name
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var deduped = refs
                .Where(r => seen.Add(r.Display ?? Guid.NewGuid().ToString()))
                .ToList();

            return deduped;
        }

        private static void TryAddAssemblyRef(Assembly asm, List<MetadataReference> refs, List<string> diagnostics)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(asm.Location))
                    refs.Add(MetadataReference.CreateFromFile(asm.Location));
            }
            catch (Exception ex)
            {
                diagnostics.Add($"[Warning] Could not add reference for '{asm.GetName().Name}': {ex.Message}");
            }
        }

        private static void TryAddPathRef(string path, List<MetadataReference> refs, List<string> diagnostics)
        {
            try
            {
                refs.Add(MetadataReference.CreateFromFile(path));
            }
            catch (Exception ex)
            {
                diagnostics.Add($"[Warning] Could not add reference from path '{path}': {ex.Message}");
            }
        }
    }
}
