using System.Text.Json.Serialization;

namespace EerieLeap.Domain.SensorDomain.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SensorType {
    Physical,
    Virtual
}
