using System.Text.Json.Serialization;

namespace AutoSensorMonitor.Types;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ConversionType
{
    Linear,
    Expression,
    Virtual
}