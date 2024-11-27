using System.Text.Json.Serialization;

namespace EerieLeap.Types;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SensorType {
    Physical,
    Virtual
}
