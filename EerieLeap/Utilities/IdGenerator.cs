using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.ComponentModel.DataAnnotations;

namespace EerieLeap.Utilities;

public static class IdGenerator {

    private const int GeneratedIdLength = 6;

    private static readonly Regex _invalidCharsRegex = new("[^a-z0-9_]", RegexOptions.Compiled);
    private static readonly Regex _multipleSpacesRegex = new(@"\s+", RegexOptions.Compiled);

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "ToLowerInvariant is preferred for identifiers")]
    public static string GenerateId(string? name = null) {
        if (string.IsNullOrWhiteSpace(name))
            return GenerateRandomId(GeneratedIdLength);

        var id = name.Trim().ToLowerInvariant();
        id = _multipleSpacesRegex.Replace(id, " ");
        id = id.Replace(' ', '_');
        id = _invalidCharsRegex.Replace(id, "");

        return string.IsNullOrEmpty(id)
            ? GenerateRandomId(GeneratedIdLength)
            : id;
    }

    private static string GenerateRandomId(int length) {
        const string chars = "abcdef0123456789";
        using var rng = RandomNumberGenerator.Create();
        var buffer = new byte[length];
        rng.GetBytes(buffer);

        var result = new char[length];
        for (int i = 0; i < buffer.Length; i++)
            result[i] = chars[buffer[i] % chars.Length];

        return new string(result);
    }
}
