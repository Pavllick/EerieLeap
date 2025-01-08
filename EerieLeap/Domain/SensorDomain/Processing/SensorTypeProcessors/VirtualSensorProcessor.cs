using System.ComponentModel.DataAnnotations;
using EerieLeap.Domain.SensorDomain.Models;
using EerieLeap.Utilities;

namespace EerieLeap.Domain.SensorDomain.Processing.SensorTypeProcessors;

internal partial class VirtualSensorProcessor : ISensorReadingProcessor {
    private readonly ILogger _logger;
    private readonly SensorReadingBuffer _buffer;

    public VirtualSensorProcessor(ILogger logger, [Required] SensorReadingBuffer buffer) {
        _logger = logger;
        _buffer = buffer;
    }

    public async Task ProcessReadingAsync([Required] SensorReading reading) {
        var sensor = reading.Sensor.Configuration;

        if (string.IsNullOrEmpty(sensor.ConversionExpression)) {
            LogExpressionNotSpecified(reading.Sensor.Id.Value);
            return;
        }

        try {
            // Get latest readings for all dependencies
            var sensorIds = ExpressionEvaluator.ExtractSensorIds(sensor.ConversionExpression);
            var readings = _buffer.GetAllReadings();

            var sensorValues = readings
                .Where(r => r != null)
                .ToDictionary(
                    r => r!.Sensor.Id.Value,
                    r => r!.Value
                );

            var value = ExpressionEvaluator.Evaluate(
                sensor.ConversionExpression,
                sensorValues);

            reading.UpdateValue(value);
            _buffer.AddReading(reading);
        } catch (Exception ex) when (ex is ArgumentException or InvalidOperationException) {
            LogVirtualSensorError(reading.Sensor.Id.Value, ex.Message);
            LogExceptionDetails(ex);
            reading.MarkAsError(ex.Message);
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    #region Loggers

    [LoggerMessage(Level = LogLevel.Warning, Message = "Expression not specified for virtual sensor {sensorId}")]
    private partial void LogExpressionNotSpecified(string sensorId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to process virtual sensor {sensorId}. {exceptionMessage}")]
    private partial void LogVirtualSensorError(string sensorId, string exceptionMessage);

    // Debug loggers

    [LoggerMessage(Level = LogLevel.Debug)]
    private partial void LogExceptionDetails(Exception ex);

    #endregion
}
