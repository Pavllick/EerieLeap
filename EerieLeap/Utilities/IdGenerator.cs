using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;

namespace EerieLeap.Utilities;

public static class IdGenerator {
    private static readonly Regex _invalidCharsRegex = new("[^a-z0-9_]", RegexOptions.Compiled);
    private static readonly Regex _multipleSpacesRegex = new(@"\s+", RegexOptions.Compiled);

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "ToLowerInvariant is preferred for identifiers")]
    public static string GenerateId(string? name = null) {
        if (string.IsNullOrWhiteSpace(name))
            return Guid.NewGuid().ToString("N")[..6];

        // Convert to lowercase and trim
        var id = name.Trim().ToLowerInvariant();

        // Replace multiple spaces with single space
        id = _multipleSpacesRegex.Replace(id, " ");

        // Replace spaces with underscores
        id = id.Replace(' ', '_');

        // Remove any other invalid characters
        id = _invalidCharsRegex.Replace(id, "");

        // If after cleaning the ID is empty, generate a GUID-based one
        if (string.IsNullOrEmpty(id))
            return Guid.NewGuid().ToString("N")[..6];

        return id;
    }
}
