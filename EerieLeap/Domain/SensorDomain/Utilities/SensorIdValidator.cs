using System.Text.RegularExpressions;

namespace EerieLeap.Domain.SensorDomain.Utilities;

public static class SensorIdValidator {
    private static readonly Regex _validIdPattern = new("^[a-zA-Z0-9_]+$", RegexOptions.Compiled);

    public static bool IsValid(string? id) =>
        !string.IsNullOrWhiteSpace(id) && _validIdPattern.IsMatch(id);
}
