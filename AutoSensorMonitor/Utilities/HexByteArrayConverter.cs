using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoSensorMonitor.Utilities;

public sealed class HexByteArrayConverter : JsonConverter<byte[]>
{
    public override byte[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Expected string value for byte array");
        }

        string? hex = reader.GetString();
        if (string.IsNullOrEmpty(hex))
        {
            return Array.Empty<byte>();
        }

        // Remove 0x prefix if present
        hex = hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase) 
            ? hex[2..] 
            : hex;

        // Handle odd-length hex strings by padding with leading zero
        if (hex.Length % 2 == 1)
        {
            hex = "0" + hex;
        }

        try
        {
            return Convert.FromHexString(hex);
        }
        catch (FormatException)
        {
            throw new JsonException($"Invalid hex string: {hex}");
        }
    }

    public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
    {
        writer.WriteStringValue("0x" + Convert.ToHexString(value).ToLower());
    }
}
