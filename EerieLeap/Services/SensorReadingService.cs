using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using EerieLeap.Configuration;
using EerieLeap.Types;
using EerieLeap.Utilities;

namespace EerieLeap.Services;

public sealed partial class SensorReadingService : BackgroundService, ISensorReadingService {
    private readonly ILogger _logger;
    private readonly IAdcConfigurationService _adcService;
    private readonly ISensorConfigurationService _sensorsConfigService;
    private readonly AsyncLock _asyncLock = new();
    private readonly ConcurrentDictionary<string, double> _lastReadings = new();
    private TaskCompletionSource<bool>? _initializationTcs;

    public SensorReadingService(ILogger logger, [Required] IAdcConfigurationService adcService, [Required] ISensorConfigurationService sensorsConfigService) {
        _logger = logger;
        _adcService = adcService;
        _sensorsConfigService = sensorsConfigService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        _initializationTcs = new TaskCompletionSource<bool>();

        try {
            await _adcService.InitializeAsync().ConfigureAwait(false);
            await _sensorsConfigService.InitializeAsync().ConfigureAwait(false);

            _initializationTcs.TrySetResult(true);

            while (!stoppingToken.IsCancellationRequested) {
                try {
                    await UpdateReadingsAsync().ConfigureAwait(false);
                    await Task.Delay(1000, stoppingToken).ConfigureAwait(false);
                } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                    // Normal shutdown, no need to log error
                    break;
                } catch (Exception ex) {
                    LogUpdateReadingsError(ex);
                    throw;
                }
            }
        } catch (Exception ex) {
            _initializationTcs.TrySetException(ex);
            throw;
        }
    }

    public async Task<IEnumerable<ReadingResult>> GetReadingsAsync() {
        using var releaser = await _asyncLock.LockAsync().ConfigureAwait(false);

        return _lastReadings.Select(kvp => new ReadingResult {
            Id = kvp.Key,
            Value = kvp.Value
        });
    }

    public async Task<ReadingResult?> GetReadingAsync([Required] string id) {
        using var releaser = await _asyncLock.LockAsync().ConfigureAwait(false);

        return _lastReadings.TryGetValue(id, out var value)
            ? new ReadingResult { Id = id, Value = value }
            : null;
    }

    private async Task UpdateReadingsAsync() {
        using var releaser = await _asyncLock.LockAsync().ConfigureAwait(false);

        Dictionary<string, double> newReadings = new();
        var adc = _adcService.GetAdc()!;
        var sensors = _sensorsConfigService.GetConfigurations()!;

        // First process ADC sensors
        foreach (var sensor in sensors.Where(s => s.Type != SensorType.Virtual)) {
            try {
                if (sensor.Channel == null) {
                    LogChannelNotSpecified(sensor.Name, null);
                    continue;
                }
                var voltage = await adc.ReadChannelAsync(sensor.Channel.Value).ConfigureAwait(false);
                newReadings[sensor.Id] = ConvertVoltageToValue(voltage, sensor);
            } catch (InvalidOperationException ex) {
                LogReadSensorError(sensor.Name, ex);
            } catch (ArgumentOutOfRangeException ex) {
                LogReadSensorError(sensor.Name, ex);
            } catch (TimeoutException ex) {
                LogReadSensorError(sensor.Name, ex);
            }
        }

        // Then process virtual sensors
        foreach (var sensor in sensors.Where(s => s.Type == SensorType.Virtual)) {
            try {
                if (string.IsNullOrEmpty(sensor.ConversionExpression)) {
                    LogExpressionNotSpecified(sensor.Name, null);
                    continue;
                }

                // Extract sensor IDs from expression and create value dictionary
                var sensorIds = ExpressionEvaluator.ExtractSensorIds(sensor.ConversionExpression);
                var sensorValues = sensorIds.ToDictionary(id => id,
                    id => newReadings.TryGetValue(id, out var value) ? value : 0.0);

                newReadings[sensor.Id] = ExpressionEvaluator.EvaluateWithSensors(
                    sensor.ConversionExpression,
                    sensorValues);
            } catch (ArgumentException ex) {
                LogVirtualSensorError(sensor.Name, ex);
            } catch (InvalidOperationException ex) {
                LogVirtualSensorError(sensor.Name, ex);
            }
        }

        _lastReadings.Clear();
        foreach (var (id, value) in newReadings)
            _lastReadings.AddOrUpdate(id, value, (_, _) => value);
    }

    private static double ConvertVoltageToValue([Required] double voltage, [Required] SensorConfig sensor) {
        if (!string.IsNullOrEmpty(sensor.ConversionExpression)) {
            try {
                return ExpressionEvaluator.Evaluate(sensor.ConversionExpression, voltage);
            } catch (Exception ex) {
                throw new InvalidOperationException(
                    $"Failed to evaluate conversion expression for sensor {sensor.Name}: {ex.Message}");
            }
        }

        // Ensure all required values are present for linear conversion
        if (!sensor.MinVoltage.HasValue || !sensor.MaxVoltage.HasValue ||
            !sensor.MinValue.HasValue || !sensor.MaxValue.HasValue) {
            throw new InvalidOperationException(
                $"Sensor {sensor.Name} is missing required voltage or value range configuration");
        }

        // Default linear conversion
        var voltageRange = sensor.MaxVoltage.Value - sensor.MinVoltage.Value;
        var valueRange = sensor.MaxValue.Value - sensor.MinValue.Value;

        return ((voltage - sensor.MinVoltage.Value) / voltageRange * valueRange) + sensor.MinValue.Value;
    }

    // Needed for unit tests to wait for initialization
    public Task WaitForInitializationAsync() =>
        _initializationTcs?.Task ?? Task.CompletedTask;

    public sealed override void Dispose() {
        _asyncLock.Dispose();
        base.Dispose();
    }

    #region Loggers

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to update sensor readings")]
    private partial void LogUpdateReadingsError(Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to read sensor {name}")]
    private partial void LogReadSensorError(string name, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to evaluate virtual sensor {name}")]
    private partial void LogVirtualSensorError(string name, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Channel not specified for sensor {name}")]
    private partial void LogChannelNotSpecified(string name, Exception? ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Expression not specified for virtual sensor {name}")]
    private partial void LogExpressionNotSpecified(string name, Exception? ex);

    #endregion
}
