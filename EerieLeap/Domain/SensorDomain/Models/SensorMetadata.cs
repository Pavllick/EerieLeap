namespace EerieLeap.Domain.SensorDomain.Models;

public record SensorMetadata {
    public string Name { get; }
    public string Unit { get; }
    public int SamplingRateMs { get; }

    public SensorMetadata(string name, string unit, int samplingRateMs) {
        Name = name;
        Unit = unit;
        SamplingRateMs = samplingRateMs;
    }
}
