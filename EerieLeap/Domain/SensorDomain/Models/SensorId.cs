namespace EerieLeap.Domain.SensorDomain.Models;

public record SensorId {
    public string Value { get; }

    public SensorId(string value) {
        if (string.IsNullOrEmpty(value))
            throw new ArgumentException("Sensor ID cannot be empty", nameof(value));

        Value = value;
    }
}
