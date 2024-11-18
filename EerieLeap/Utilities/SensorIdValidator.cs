using System.Text.RegularExpressions;

namespace EerieLeap.Utilities;

public static class SensorIdValidator
{
    private static readonly Regex ValidIdPattern = new("^[a-zA-Z0-9_]+$", RegexOptions.Compiled);

    public static bool IsValid(string? id) =>
        !string.IsNullOrWhiteSpace(id) && ValidIdPattern.IsMatch(id);
}
