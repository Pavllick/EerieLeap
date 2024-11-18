using System.Text.RegularExpressions;

namespace EerieLeap.Utilities;

public static class IdGenerator
{
    private static readonly Regex InvalidCharsRegex = new("[^a-z0-9_]", RegexOptions.Compiled);
    private static readonly Regex MultipleSpacesRegex = new(@"\s+", RegexOptions.Compiled);
    
    public static string GenerateId(string? name = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Guid.NewGuid().ToString("N")[..6];
            
        // Convert to lowercase and trim
        var id = name.Trim().ToLowerInvariant();
        
        // Replace multiple spaces with single space
        id = MultipleSpacesRegex.Replace(id, " ");
        
        // Replace spaces with underscores
        id = id.Replace(' ', '_');
        
        // Remove any other invalid characters
        id = InvalidCharsRegex.Replace(id, "");

        // If after cleaning the ID is empty, generate a GUID-based one
        if (string.IsNullOrEmpty(id))
            return Guid.NewGuid().ToString("N")[..6];
            
        return id;
    }
}
