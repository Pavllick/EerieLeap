using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EerieLeap.Utilities.Converters;

public sealed class HexByteArrayJsonConverter : JsonConverter<byte[]?> {
    public override bool HandleNull => true;

    public override byte[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType == JsonTokenType.Null)
            return Array.Empty<byte>(); // Return empty array if value is null

        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected start of array.");

        var byteList = new List<byte>();

        while (reader.Read()) {
            if (reader.TokenType == JsonTokenType.EndArray)
                return byteList.ToArray();

            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException("Expected string values in array.");

            var hexString = reader.GetString();
            if (string.IsNullOrWhiteSpace(hexString) || !hexString.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                throw new JsonException("Expected hex string starting with '0x'.");

            if (!byte.TryParse(hexString.AsSpan(2), NumberStyles.HexNumber, null, out var byteValue))
                throw new JsonException($"Invalid hex value: {hexString}");

            byteList.Add(byteValue);
        }

        throw new JsonException("Unexpected end of JSON.");
    }

    public override void Write([Required] Utf8JsonWriter writer, byte[]? value, JsonSerializerOptions options) {
        if (value == null) {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartArray();

        foreach (var byteValue in value)
            writer.WriteStringValue($"0x{byteValue:X2}");

        writer.WriteEndArray();
    }
}
