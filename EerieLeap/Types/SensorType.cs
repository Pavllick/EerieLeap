using System.Text.Json.Serialization;

namespace EerieLeap.Types;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SensorType
{
    Temperature,
    Pressure,
    Flow,
    Voltage,
    Current,
    Virtual
}
