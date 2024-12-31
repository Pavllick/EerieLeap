using System.Text.Json.Serialization;
using EerieLeap.Utilities.Converters;

namespace EerieLeap.Domain.SensorDomain.Models;

[JsonConverter(typeof(EnumJsonConverter<ReadingStatus>))]
public enum ReadingStatus {
    Raw,
    Validated,
    Processed,
    Error
}
