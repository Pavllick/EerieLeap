using EerieLeap.Types;

namespace EerieLeap.Tests.Functional.Models;

/// <summary>
/// Test request model that mirrors SensorConfig for testing validation scenarios.
/// </summary>
public record SensorConfigRequest {
    public string? Id { get; init; }
    public string? Name { get; init; }
    public SensorType? Type { get; init; }
    public string? Unit { get; init; }
    public int? Channel { get; init; }
    public double? MinVoltage { get; init; }
    public double? MaxVoltage { get; init; }
    public double? MinValue { get; init; }
    public double? MaxValue { get; init; }
    public int? SamplingRateMs { get; init; }
    public string? ConversionExpression { get; init; }

    /// <summary>
    /// Creates a valid physical sensor configuration request.
    /// </summary>
    public static SensorConfigRequest CreateValidPhysical() => new() {
        Id = "test_sensor",
        Name = "Physical Sensor",
        Type = SensorType.Physical,
        Unit = "°C",
        Channel = 0,
        MinVoltage = 0.0,
        MaxVoltage = 3.3,
        MinValue = 0,
        MaxValue = 100,
        SamplingRateMs = 1000
    };

    /// <summary>
    /// Creates a valid virtual sensor configuration request.
    /// </summary>
    public static SensorConfigRequest CreateValidVirtual() => new() {
        Id = "virtual_sensor",
        Name = "Virtual Sensor",
        Type = SensorType.Virtual,
        Unit = "°C",
        SamplingRateMs = 1000,
        ConversionExpression = "({test_sensor} * 2) + 1"
    };
}
