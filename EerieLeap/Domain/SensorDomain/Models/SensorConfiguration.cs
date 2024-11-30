using EerieLeap.Types;

namespace EerieLeap.Domain.SensorDomain.Models;

public record SensorConfiguration {
    public SensorType Type { get; }
    public int? Channel { get; }
    public CalibrationData? Calibration { get; }
    public string? ConversionExpression { get; }

    public SensorConfiguration(SensorType type, int? channel, CalibrationData? calibration, string? conversionExpression) {
        if (type == SensorType.Physical && !channel.HasValue)
            throw new ArgumentException("Physical sensors must have a channel specified");

        if (type == SensorType.Physical && calibration == null)
            throw new ArgumentException("Physical sensors must have calibration data");

        if (type == SensorType.Virtual && string.IsNullOrEmpty(conversionExpression))
            throw new ArgumentException("Virtual sensors must have a conversion expression");

        Type = type;
        Channel = channel;
        Calibration = calibration;
        ConversionExpression = conversionExpression;
    }
}
