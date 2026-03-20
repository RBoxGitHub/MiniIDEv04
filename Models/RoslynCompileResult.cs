using System.Collections.Generic;
using System.Reflection;

namespace MiniIDEv04.Models
{
    /// <summary>
    /// Result returned by RoslynCompiler.CompileAsync().
    /// Never throws — all errors are surfaced here.
    /// </summary>
    public class RoslynCompileResult
    {
        /// <summary>True if compilation succeeded and Assembly is loaded.</summary>
        public bool Success { get; init; }

        /// <summary>The compiled assembly, or null if compilation failed.</summary>
        public Assembly? Assembly { get; init; }

        /// <summary>
        /// List of human-readable diagnostic messages (errors and warnings).
        /// Always populated on failure; may contain warnings on success.
        /// </summary>
        public IReadOnlyList<string> Diagnostics { get; init; } = [];

        /// <summary>Convenience factory for a failed result.</summary>
        public static RoslynCompileResult Fail(IReadOnlyList<string> diagnostics) =>
            new() { Success = false, Diagnostics = diagnostics };

        /// <summary>Convenience factory for a successful result.</summary>
        public static RoslynCompileResult Ok(Assembly assembly, IReadOnlyList<string> diagnostics) =>
            new() { Success = true, Assembly = assembly, Diagnostics = diagnostics };
    }
}
