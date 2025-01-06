using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace EerieLeap.Utilities.IO;
public static class PathResolver {
    public static string? GetDirectory([CallerFilePath][Required] string callerFilePath = null) =>
        Path.GetDirectoryName(callerFilePath);

    public static string GetFullPath([Required] string callerRelativePath, [CallerFilePath] string callerFilePath = null) =>
        Path.Combine(GetDirectory(callerFilePath), callerRelativePath);
}
