using System.Text.Json;
using System.Text.Json.Serialization;

namespace EerieLeap.Utilities;

public sealed class HexByteArrayConverter : JsonConverter<byte[]?>
{
    public override bool HandleNull => true;

    public override byte[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return Array.Empty<byte>();

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected string value for byte array");

        string? hex = reader.GetString();
        if (hex == null)
            return Array.Empty<byte>();

        hex = hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase) 
            ? hex[2..]
            : hex;

        if (string.IsNullOrEmpty(hex))
            return Array.Empty<byte>();

        // Handle odd-length hex strings by padding with leading zero
        if (hex.Length % 2 == 1)
            hex = "0" + hex;

        try
        {
            return Convert.FromHexString(hex);
        }
        catch (FormatException)
        {
            throw new JsonException($"Invalid hex string: {hex}");
        }
    }

    public override void Write(Utf8JsonWriter writer, byte[]? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }
        
        if (value.Length == 0)
        {
            writer.WriteStringValue("0x");
            return;
        }
        
        writer.WriteStringValue("0x" + Convert.ToHexString(value).ToLower());
    }
}
