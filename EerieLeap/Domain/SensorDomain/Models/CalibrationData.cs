namespace EerieLeap.Domain.SensorDomain.Models;

public record CalibrationData {
    public double MinVoltage { get; }
    public double MaxVoltage { get; }
    public double MinValue { get; }
    public double MaxValue { get; }

    public CalibrationData(double minVoltage, double maxVoltage, double minValue, double maxValue) {
        if (minVoltage >= maxVoltage)
            throw new ArgumentException("MinVoltage must be less than MaxVoltage");
        if (minValue >= maxValue)
            throw new ArgumentException("MinValue must be less than MaxValue");

        MinVoltage = minVoltage;
        MaxVoltage = maxVoltage;
        MinValue = minValue;
        MaxValue = maxValue;
    }
}
