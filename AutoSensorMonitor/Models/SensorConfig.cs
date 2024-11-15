using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AutoSensorMonitor.Service.Models;

public sealed class SensorConfig 
{
    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SensorType Type { get; init; }

    [Required]
    public string Name { get; init; } = null!;

    [Range(0, int.MaxValue)]
    public int Channel { get; init; }

    [Range(0, double.MaxValue)]
    public double MinVoltage { get; init; }

    [Range(0, double.MaxValue)]
    public double MaxVoltage { get; init; }

    [Range(double.MinValue, double.MaxValue)]
    public double MinValue { get; init; }

    [Range(double.MinValue, double.MaxValue)]
    public double MaxValue { get; init; }

    public double ConversionFactor { get; init; } = 1.0;

    [Required]
    public string Unit { get; init; } = null!;

    [Range(1, int.MaxValue)]
    public int SamplingRateMs { get; init; } = 1000;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SensorType 
{
    Temperature,
    Pressure,
    Other,
    Flow,
    Level
}
