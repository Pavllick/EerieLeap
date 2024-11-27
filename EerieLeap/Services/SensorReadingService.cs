using EerieLeap.Hardware;
using EerieLeap.Configuration;
using EerieLeap.Types;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using EerieLeap.Utilities;

namespace EerieLeap.Services;

public sealed partial class SensorReadingService : BackgroundService, ISensorReadingService {
    private readonly ILogger _logger;
    private readonly IAdcConfigurationService _adcService;
    private readonly string _configPath;
    private readonly AsyncLock _asyncLock = new();
    private readonly JsonSerializerOptions _writeOptions = new() { WriteIndented = true };
    private readonly JsonSerializerOptions _readOptions = new() {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
    private List<SensorConfig> _configs;
    // private readonly IAdc? _adc;
    private Dictionary<string, double> _lastReadings = new();
    private TaskCompletionSource<bool>? _initializationTcs;

    public SensorReadingService(ILogger logger, IAdcConfigurationService adcService, IConfiguration configuration) {
        _logger = logger;
        _adcService = adcService;
        _configPath = configuration.GetValue<string>("ConfigurationPath") ?? throw new ArgumentException("ConfigurationPath not set in configuration");
        _configs = new List<SensorConfig>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        _initializationTcs = new TaskCompletionSource<bool>();

        await _adcService.InitializeAdcAsync().ConfigureAwait(false);

        try {
            await LoadConfigurationAsync().ConfigureAwait(false);
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

    public Task WaitForInitializationAsync() =>
        _initializationTcs?.Task ?? Task.CompletedTask;

    public async Task<IEnumerable<ReadingResult>> GetReadingsAsync() {
        using var releaser = await _asyncLock.LockAsync().ConfigureAwait(false);

        return _lastReadings.Select(kvp => new ReadingResult {
            Id = kvp.Key,
            Value = kvp.Value
        });
    }

    public async Task<ReadingResult?> GetReadingAsync(string id) {
        using var releaser = await _asyncLock.LockAsync().ConfigureAwait(false);

        return _lastReadings.TryGetValue(id, out var value)
            ? new ReadingResult { Id = id, Value = value }
            : null;
    }

    public async Task<IEnumerable<SensorConfig>> GetSensorConfigurationsAsync() {
        using var releaser = await _asyncLock.LockAsync().ConfigureAwait(false);

        return _configs;
    }

    public async Task UpdateConfigurationAsync([Required] IEnumerable<SensorConfig> configs) {
        using var releaser = await _asyncLock.LockAsync().ConfigureAwait(false);

        var configsList = configs.ToList();

        _lastReadings.Clear(); // TODO: Should we clear readings only for changed sensors

        var json = JsonSerializer.Serialize(configsList, _writeOptions);
        await File.WriteAllTextAsync(Path.Combine(_configPath, "sensors.json"), json).ConfigureAwait(false);

        _configs = configsList;
        LogSensorConfigsUpdated();
    }

    private async Task UpdateReadingsAsync() {
        Dictionary<string, double> newReadings = new();
        var adc = await _adcService.GetAdcAsync().ConfigureAwait(false);
        var sensors = _configs.ToArray();

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

        using (var releaser = await _asyncLock.LockAsync().ConfigureAwait(false)) {
            _lastReadings = newReadings;
        }
    }

    private async Task LoadConfigurationAsync() {
        using var releaser = await _asyncLock.LockAsync().ConfigureAwait(false);

        try {
            var configPath = Path.Combine(_configPath, "sensors.json");

            List<SensorConfig> configs;

            if (File.Exists(configPath)) {
                var json = await File.ReadAllTextAsync(configPath).ConfigureAwait(false);
                configs = JsonSerializer.Deserialize<List<SensorConfig>>(json, _readOptions) ?? throw new JsonException("Failed to deserialize sensor configs");
            } else {
                configs = CreateDefaultSensorConfigs();

                Directory.CreateDirectory(_configPath);
                var json = JsonSerializer.Serialize(configs, _writeOptions);
                await File.WriteAllTextAsync(configPath, json).ConfigureAwait(false);

                LogDefaultSensorConfigsUsed();
            }

            _configs = configs;
        } catch (Exception ex) {
            LogConfigurationLoadError(ex);
            throw;
        }
    }

    private static List<SensorConfig> CreateDefaultSensorConfigs() =>
        new List<SensorConfig> {
            new() {
                Id = "coolant_temp",
                Name = "Coolant Temperature",
                Type = SensorType.Physical,
                Channel = 0,
                MinVoltage = 0.5,
                MaxVoltage = 4.5,
                MinValue = 0,
                MaxValue = 120,
                Unit = "°C",
                SamplingRateMs = 1000
            },
            new() {
                Id = "oil_temp",
                Name = "Oil Temperature",
                Type = SensorType.Physical,
                Channel = 1,
                MinVoltage = 0.5,
                MaxVoltage = 4.5,
                MinValue = 0,
                MaxValue = 150,
                Unit = "°C",
                SamplingRateMs = 1000
            },
            new() {
                Id = "avg_temp",
                Name = "Average Temperature",
                Type = SensorType.Virtual,
                MinValue = 0,
                MaxValue = 150,
                Unit = "°C",
                SamplingRateMs = 1000,
                ConversionExpression = "({coolant_temp} + {oil_temp}) / 2 * Sin(PI/4)"
            }
        };

    private static double ConvertVoltageToValue(double voltage, SensorConfig sensor) {
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

    public sealed override void Dispose() {
        _asyncLock.Dispose();
        base.Dispose();
    }

    #region Loggers

    [LoggerMessage(Level = LogLevel.Error, EventId = 1, Message = "Failed to update sensor readings")]
    private partial void LogUpdateReadingsError(Exception ex);

    [LoggerMessage(Level = LogLevel.Error, EventId = 2, Message = "Failed to read sensor {name}")]
    private partial void LogReadSensorError(string name, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, EventId = 3, Message = "Failed to evaluate virtual sensor {name}")]
    private partial void LogVirtualSensorError(string name, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, EventId = 4, Message = "Channel not specified for sensor {name}")]
    private partial void LogChannelNotSpecified(string name, Exception? ex);

    [LoggerMessage(Level = LogLevel.Warning, EventId = 5, Message = "Expression not specified for virtual sensor {name}")]
    private partial void LogExpressionNotSpecified(string name, Exception? ex);

    [LoggerMessage(Level = LogLevel.Error, EventId = 6, Message = "Failed to load Sensor configurations")]
    private partial void LogConfigurationLoadError(Exception ex);

    [LoggerMessage(Level = LogLevel.Information, EventId = 7, Message = "Using default sensor configurations")]
    private partial void LogDefaultSensorConfigsUsed();

    [LoggerMessage(Level = LogLevel.Information, EventId = 8, Message = "Updated sensor configurations")]
    private partial void LogSensorConfigsUpdated();

    #endregion
}
