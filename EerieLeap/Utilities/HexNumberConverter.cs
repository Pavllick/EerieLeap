using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;
using System.ComponentModel.DataAnnotations;

namespace EerieLeap.Utilities;

public sealed class HexNumberConverter<T> : JsonConverter<T> where T : struct {
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType == JsonTokenType.String) {
            string? hex = reader.GetString();
            if (string.IsNullOrEmpty(hex))
                return default;

            // Remove 0x prefix if present
            hex = hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? hex[2..]
                : hex;

            try {
                // Parse hex string to number
                if (typeof(T) == typeof(byte))
                    return (T)(object)byte.Parse(hex, System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                if (typeof(T) == typeof(int))
                    return (T)(object)int.Parse(hex, System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture);

                throw new JsonException($"Unsupported type for hex conversion: {typeof(T)}");
            } catch (FormatException) {
                throw new JsonException($"Invalid hex string: {hex}");
            } catch (OverflowException) {
                throw new JsonException($"Hex value {hex} is too large for type {typeof(T)}");
            }
        }

        if (reader.TokenType == JsonTokenType.Number) {
            if (typeof(T) == typeof(byte))
                return (T)(object)reader.GetByte();
            if (typeof(T) == typeof(int))
                return (T)(object)reader.GetInt32();
        }

        throw new JsonException($"Expected string or number value for hex number, got {reader.TokenType}");
    }

    public override void Write([Required] Utf8JsonWriter writer, T value, JsonSerializerOptions options) {
        var hex = typeof(T) switch {
            Type t when t == typeof(byte) => $"0x{(byte)(object)value:x2}",
            Type t when t == typeof(int) => $"0x{(int)(object)value:x}",
            Type t when t == typeof(uint) => $"0x{(uint)(object)value:x}",
            Type t when t == typeof(long) => $"0x{(long)(object)value:x}",
            Type t when t == typeof(ulong) => $"0x{(ulong)(object)value:x}",
            Type t when t == typeof(short) => $"0x{(short)(object)value:x}",
            Type t when t == typeof(ushort) => $"0x{(ushort)(object)value:x}",
            _ => throw new JsonException($"Unsupported type for hex number: {typeof(T)}")
        };

        writer.WriteStringValue(hex);
    }
}
