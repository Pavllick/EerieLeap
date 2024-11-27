using System.Collections.Concurrent;
using EerieLeap.Types;
using System.ComponentModel.DataAnnotations;
using EerieLeap.Configuration;
using EerieLeap.Utilities;
using System.Text.Json;

namespace EerieLeap.Services;

public partial class SensorConfigurationService : ISensorConfigurationService {
    private readonly ConcurrentDictionary<string, SensorConfig> _configs;
    private readonly string _configPath;
    private readonly ILogger _logger;
    private readonly AsyncLock _asyncLock = new();
    private readonly JsonSerializerOptions _writeOptions = new() { WriteIndented = true };
    private readonly JsonSerializerOptions _readOptions = new() {
        PropertyNameCaseInsensitive = true
    };
    private bool _disposed;

    public SensorConfigurationService(ILogger logger, IConfiguration configuration) {
        _logger = logger;

        _configs = new ConcurrentDictionary<string, SensorConfig>();
        _configPath = configuration.GetValue<string>("ConfigurationPath")
            ?? throw new ArgumentException("ConfigurationPath not set in configuration");
    }

    public async Task InitializeAsync() =>
        await LoadConfigurationAsync().ConfigureAwait(false);

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
            foreach (var config in configs)
                _configs.AddOrUpdate(config.Id, config, (_, _) => config);
        } catch (Exception ex) {
            LogConfigurationLoadError(ex);
            throw;
        }
    }

    public IReadOnlyList<SensorConfig> GetConfigurations() {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _configs.Values.ToList().AsReadOnly();
    }

    public SensorConfig? GetConfiguration(string sensorId) {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _configs.TryGetValue(sensorId, out var config) ? config : null;
    }

    public bool TryGetConfiguration(string sensorId, out SensorConfig? config) {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _configs.TryGetValue(sensorId, out config);
    }

    public async Task<bool> UpdateConfigurationAsync([Required] IEnumerable<SensorConfig> configs) {
        using var releaser = await _asyncLock.LockAsync().ConfigureAwait(false);

        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!ValidateConfigurations(configs))
            return false;

        var configsList = configs.ToList();

        // _lastReadings.Clear(); // TODO: Should we clear readings only for changed sensors

        var json = JsonSerializer.Serialize(configsList, _writeOptions);
        await File.WriteAllTextAsync(Path.Combine(_configPath, "sensors.json"), json).ConfigureAwait(false);

        _configs.Clear();
        foreach (var config in configsList)
            _configs.AddOrUpdate(config.Id, config, (_, _) => config);

        LogSensorConfigsUpdated();

        return true;
    }

    public bool ValidateConfigurations([Required] IEnumerable<SensorConfig> configs) {
        var configList = configs.ToList();

        foreach (var config in configList) {
            if (!ValidateConfiguration(config)) {
                LogValidationConfigFailed(config?.Id ?? "null");
                return false;
            }

            if (configList.Count(c => c.Id == config.Id) > 1) {
                LogValidationDuplicateId(config.Id);
                return false;
            }

            if (config.Type == SensorType.Physical && configList.Count(c => c.Channel == config.Channel) > 1) {
                LogValidationDuplicateChannel(config.Channel, config.Id);
                return false;
            }
        }

        return true;
    }

    private bool ValidateConfiguration([Required] SensorConfig config) {
        var context = new ValidationContext(config);
        var results = new List<ValidationResult>();

        if (!Validator.TryValidateObject(config, context, results, validateAllProperties: true)) {
            foreach (var result in results)
                LogValidationError(config.Id, result.ErrorMessage ?? "Unknown validation error");
            return false;
        }

        if (config.Type == SensorType.Physical) {
            if (config.MinVoltage >= config.MaxVoltage) {
                LogValidationVoltageError(config.Id, config.MinVoltage, config.MaxVoltage);
                return false;
            }
        }

        return true;
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

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) {
        if (_disposed)
            return;

        if (disposing) {
            _asyncLock.Dispose();
        }

        _disposed = true;
    }

    #region Loggers

    [LoggerMessage(Level = LogLevel.Error, EventId = 1, Message = "Failed to load Sensor configurations")]
    private partial void LogConfigurationLoadError(Exception ex);

    [LoggerMessage(Level = LogLevel.Information, EventId = 2, Message = "Using default sensor configurations")]
    private partial void LogDefaultSensorConfigsUsed();

    [LoggerMessage(Level = LogLevel.Information, EventId = 3, Message = "Updated sensor configurations")]
    private partial void LogSensorConfigsUpdated();

    [LoggerMessage(Level = LogLevel.Warning, EventId = 4, Message = "Validation failed for sensor config with ID: {sensorId}")]
    private partial void LogValidationConfigFailed(string sensorId);

    [LoggerMessage(Level = LogLevel.Warning, EventId = 5, Message = "Validation failed: duplicate sensor ID found: {sensorId}")]
    private partial void LogValidationDuplicateId(string sensorId);

    [LoggerMessage(Level = LogLevel.Warning, EventId = 6, Message = "Validation failed: duplicate channel {channel} found for physical sensor {sensorId}")]
    private partial void LogValidationDuplicateChannel(int? channel, string sensorId);

    [LoggerMessage(Level = LogLevel.Warning, EventId = 7, Message = "Validation error for sensor {sensorId}: {error}")]
    private partial void LogValidationError(string sensorId, string error);

    [LoggerMessage(Level = LogLevel.Warning, EventId = 8, Message = "Validation failed for sensor {sensorId}: MinVoltage ({minVoltage}) must be less than MaxVoltage ({maxVoltage})")]
    private partial void LogValidationVoltageError(string sensorId, double? minVoltage, double? maxVoltage);

    #endregion
}
