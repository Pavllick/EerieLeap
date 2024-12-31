using EerieLeap.Configuration;
using System.ComponentModel.DataAnnotations;

namespace EerieLeap.Domain.SensorDomain.Models;

public class Sensor {
    public SensorId Id { get; }
    public SensorMetadata Metadata { get; }
    public SensorConfiguration Configuration { get; private set; }

    public Sensor([Required] SensorId id, [Required] SensorMetadata metadata, [Required] SensorConfiguration configuration) {
        Id = id;
        Metadata = metadata;
        Configuration = configuration;
    }

    public static Sensor FromConfig([Required] SensorConfig config) =>
        new(
            new SensorId(config.Id),
            new SensorMetadata(
                config.Name,
                config.Unit,
                config.SamplingRateMs
            ),
            new SensorConfiguration(
                config.Type,
                config.Channel,
                config.MinVoltage.HasValue && config.MaxVoltage.HasValue
                    ? new CalibrationData(
                        config.MinVoltage.Value,
                        config.MaxVoltage.Value,
                        config.MinValue ?? double.MinValue,
                        config.MaxValue ?? double.MaxValue)
                    : null,
                config.ConversionExpression
            ));

    public SensorConfig ToConfig() =>
        new SensorConfig {
            Id = Id.Value,
            Name = Metadata.Name,
            Type = Configuration.Type,
            Channel = Configuration.Channel,
            MinVoltage = Configuration.Calibration?.MinVoltage,
            MaxVoltage = Configuration.Calibration?.MaxVoltage,
            MinValue = Configuration.Calibration?.MinValue,
            MaxValue = Configuration.Calibration?.MaxValue,
            Unit = Metadata.Unit,
            SamplingRateMs = Metadata.SamplingRateMs,
            ConversionExpression = Configuration.ConversionExpression
        };

    public double ConvertVoltageToRawValue(double voltage) {
        if (Configuration.Type == SensorType.Virtual)
            throw new InvalidOperationException("Cannot convert voltage for virtual sensors");

        if (Configuration.Calibration == null)
            throw new InvalidOperationException("Calibration data is required for voltage conversion");

        var calibration = Configuration.Calibration;
        var voltageRange = calibration.MaxVoltage - calibration.MinVoltage;
        var valueRange = calibration.MaxValue - calibration.MinValue;

        return ((voltage - calibration.MinVoltage) / voltageRange * valueRange) + calibration.MinValue;
    }

    public void UpdateConfiguration([Required] SensorConfiguration newConfiguration) {
        if (newConfiguration.Type != Configuration.Type)
            throw new InvalidOperationException("Cannot change sensor type");

        Configuration = newConfiguration;
    }
}
