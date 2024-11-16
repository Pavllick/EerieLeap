using System.Text.RegularExpressions;

namespace AutoSensorMonitor.Utilities;

public static class IdGenerator
{
    private static readonly Regex InvalidCharsRegex = new("[^a-z0-9_]", RegexOptions.Compiled);
    
    public static string GenerateId(string? name = null)
    {
        if (string.IsNullOrEmpty(name))
            return Guid.NewGuid().ToString("N")[..6];
            
        // Convert to lowercase
        var id = name.ToLowerInvariant();
        
        // Replace spaces with underscores
        id = id.Replace(' ', '_');
        
        // Remove any other invalid characters
        id = InvalidCharsRegex.Replace(id, "");

        // If after cleaning the ID is empty, generate a GUID-based one
        return string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString("N")[..6] : id;
    }
}
