using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EerieLeap.Utilities.Converters;

public class EnumJsonConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum {
    public override TEnum Read(ref Utf8JsonReader reader, [Required] Type typeToConvert, JsonSerializerOptions options) {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value)) {
            throw new JsonException($"{typeToConvert.Name} cannot be null or empty.");
        }

        if (Enum.TryParse<TEnum>(value, true, out var result)) {
            return result;
        }

        var validValues = string.Join(", ", Enum.GetNames<TEnum>());
        throw new JsonException($"Invalid {typeToConvert.Name} '{value}'. Valid values are: {validValues}");
    }

    public override void Write([Required] Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}
