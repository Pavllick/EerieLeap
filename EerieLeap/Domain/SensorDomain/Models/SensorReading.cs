using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EerieLeap.Domain.SensorDomain.Models;

public class SensorReading {
    public string Id { get; }
    [JsonIgnore]
    public Sensor Sensor { get; }
    public double Value { get; private set; }
    public DateTime Timestamp { get; }
    public ReadingMetadata Metadata { get; private set; }
    public ReadingStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }

    [JsonConstructor]
    public SensorReading(string id, double value, DateTime timestamp, ReadingMetadata metadata, ReadingStatus status, string? errorMessage) {
        Id = id;
        Value = value;
        Timestamp = timestamp;
        Metadata = metadata;
        Status = status;
        ErrorMessage = errorMessage;
    }

    public SensorReading([Required] Sensor sensor, double value, DateTime timestamp) {
        Id = sensor.Id.Value;
        Sensor = sensor;
        Value = value;
        Timestamp = timestamp;
        Status = ReadingStatus.Raw;
        Metadata = new ReadingMetadata();
    }

    public void Validate() =>
        Status = ReadingStatus.Validated;

    public void MarkAsProcessed() =>
        Status = ReadingStatus.Processed;

    public void UpdateValue(double newValue) {
        Value = newValue;
        Status = ReadingStatus.Processed;
    }

    public void MarkAsError(string message) {
        Status = ReadingStatus.Error;
        ErrorMessage = message;
    }

    public void AddMetadata(string key, string value) =>
        Metadata.AddTag(key, value);
}

public record ReadingMetadata {
    public Dictionary<string, string> Tags { get; }

    public ReadingMetadata(Dictionary<string, string>? tags = null) =>
        Tags = tags ?? new Dictionary<string, string>();

    public void AddTag(string key, string value) =>
        Tags.Add(key, value);
}

public enum ReadingStatus {
    Raw,
    Validated,
    Processed,
    Error
}
