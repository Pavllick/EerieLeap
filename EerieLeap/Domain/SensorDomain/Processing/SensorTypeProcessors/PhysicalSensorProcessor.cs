using System.ComponentModel.DataAnnotations;
using System.Globalization;
using EerieLeap.Domain.SensorDomain.Models;
using EerieLeap.Utilities;
using EerieLeap.Domain.AdcDomain.Services;

namespace EerieLeap.Domain.SensorDomain.Processing.SensorTypeProcessors;

public partial class PhysicalSensorProcessor : ISensorReadingProcessor {
    private readonly IAdcConfigurationService _adcService;
    private readonly ILogger _logger;
    private readonly SensorReadingBuffer _buffer;

    public PhysicalSensorProcessor(
        ILogger logger,
        [Required] IAdcConfigurationService adcService,
        [Required] SensorReadingBuffer buffer) {
        _logger = logger;
        _adcService = adcService;
        _buffer = buffer;
    }

    public async Task ProcessReadingAsync([Required] SensorReading reading) {
        var adc = _adcService.GetAdc()!;
        var config = reading.Sensor.Configuration;

        if (config.Channel == null) {
            LogChannelNotSpecified(reading.Sensor.Id.Value);
            reading.MarkAsError("Channel not specified");
            return;
        }

        try {
            var voltage = await adc.ReadChannelAsync(config.Channel.Value).ConfigureAwait(false);
            var rawValue = reading.Sensor.ConvertVoltageToRawValue(voltage);

            var sensorIds = ExpressionEvaluator.ExtractSensorIds(config.ConversionExpression ?? string.Empty);
            var readings = _buffer.GetAllReadings();

            var sensorValues = readings
                .Where(r => r != null)
                .ToDictionary(
                    r => r!.Sensor.Id.Value,
                    r => r!.Value
                );

            var value = ExpressionEvaluator.Evaluate(
                config.ConversionExpression ?? string.Empty,
                rawValue,
                sensorValues);

            reading.UpdateValue(value);
            reading.AddMetadata("voltage", voltage.ToString(CultureInfo.InvariantCulture));
            reading.AddMetadata("raw_value", rawValue.ToString(CultureInfo.InvariantCulture));

            _buffer.AddReading(reading);
        } catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException or TimeoutException) {
            LogReadSensorError(reading.Sensor.Id.Value, ex);
            reading.MarkAsError(ex.Message);
        }
    }

    #region Loggers
    [LoggerMessage(Level = LogLevel.Warning, Message = "Channel not specified for physical sensor {sensorId}")]
    private partial void LogChannelNotSpecified(string sensorId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to read physical sensor {sensorId}")]
    private partial void LogReadSensorError(string sensorId, Exception ex);
    #endregion
}
