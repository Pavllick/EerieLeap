namespace EerieLeap.Utilities;

public static class StringExtensions
{
    public static string SpaceCamelCase(this string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var result = text[0].ToString();
        for (int i = 1; i < text.Length; i++)
        {
            if (char.IsUpper(text[i]))
                result += " " + char.ToLower(text[i]);
            else
                result += text[i];
        }
        
        return result;
    }
}
