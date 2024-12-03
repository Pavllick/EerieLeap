using System.Text.Json.Serialization;
using EerieLeap.Utilities.Converters;

namespace EerieLeap.Domain.SensorDomain.Models;

[JsonConverter(typeof(EnumJsonConverter<SensorType>))]
public enum SensorType {
    Physical,
    Virtual
}
