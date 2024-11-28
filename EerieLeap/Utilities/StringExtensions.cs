using System.Diagnostics.CodeAnalysis;

namespace EerieLeap.Utilities;

public static class StringExtensions {

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Required for casing")]

    public static string SpaceCamelCase(this string text) {
        if (string.IsNullOrEmpty(text))
            return text;

        var result = text[0].ToString().ToLowerInvariant();
        var isInAcronym = char.IsUpper(text[0]) && text.Length > 1 && char.IsUpper(text[1]);

        for (int i = 1; i < text.Length; i++) {
            var currentIsUpper = char.IsUpper(text[i]);
            var nextIsUpper = i + 1 < text.Length && char.IsUpper(text[i + 1]);
            var prevIsUpper = i > 0 && char.IsUpper(text[i - 1]);

            // Handle transitions between acronyms and regular words
            if (currentIsUpper) {
                // Start of new word (not in acronym)
                if (!isInAcronym && !prevIsUpper) {
                    result += " ";
                }
                // End of acronym
                else if (isInAcronym && !nextIsUpper && i + 1 < text.Length) {
                    result += " ";
                    isInAcronym = false;
                }
                // Start of acronym
                else if (!isInAcronym && nextIsUpper) {
                    if (result[^1] != ' ')
                        result += " ";
                    isInAcronym = true;
                }
                // Middle of acronym - just add space between letters
                else if (isInAcronym) {
                    result += " ";
                }
            }

            result += char.ToLowerInvariant(text[i]);

            // Update acronym state
            if (currentIsUpper && nextIsUpper)
                isInAcronym = true;
            else if (!nextIsUpper)
                isInAcronym = false;
        }

        return result;
    }
}
