using System.Collections.Concurrent;
using EerieLeap.Types;
using System.ComponentModel.DataAnnotations;
using EerieLeap.Configuration;
using EerieLeap.Repositories;
using EerieLeap.Utilities;
using EerieLeap.Domain.SensorDomain.Models;

namespace EerieLeap.Domain.SensorDomain.Services;

public partial class SensorConfigurationService : ISensorConfigurationService {
    private const string ConfigName = "sensors";
    private readonly ILogger _logger;
    private readonly IConfigurationRepository _repository;
    private readonly ConcurrentDictionary<string, Sensor> _sensors = new();
    private readonly AsyncLock _asyncLock = new();
    private bool _disposed;

    public SensorConfigurationService(ILogger logger, [Required] IConfigurationRepository repository) {
        _logger = logger;
        _repository = repository;
    }

    public async Task InitializeAsync() =>
        await LoadConfigurationAsync().ConfigureAwait(false);

    public IReadOnlyList<SensorConfig> GetConfigurations() {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _sensors.Values
            .Select(s => s.ToConfig())
            .ToList()
            .AsReadOnly();
    }

    public SensorConfig? GetConfiguration([Required] string sensorId) {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _sensors.TryGetValue(sensorId, out var sensor)
            ? sensor.ToConfig()
            : null;
    }

    public async Task<ConfigurationResult> UpdateConfigurationAsync([Required] IEnumerable<SensorConfig> configs) {
        using var releaser = await _asyncLock.LockAsync().ConfigureAwait(false);
        ObjectDisposedException.ThrowIf(_disposed, this);

        try {
            var configsList = configs.ToList();

            // Check for duplicate sensor IDs
            var duplicateIds = configsList
                .GroupBy(c => c.Id)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateIds.Count > 0) {
                var duplicateErrors = duplicateIds.Select(id =>
                    new ConfigurationError(id, "Duplicate sensor ID"));

                LogValidationError("Multiple sensors", "Duplicate sensor IDs found: " + string.Join(", ", duplicateIds));
                return ConfigurationResult.CreateFailure(duplicateErrors);
            }

            var sensorsDict = new Dictionary<string, Sensor>();
            var validationErrors = new List<ConfigurationError>();

            foreach (var config in configsList) {
                try {
                    var sensor = Sensor.FromConfig(config);
                    sensorsDict[sensor.Id.Value] = sensor;
                } catch (ArgumentException ex) {
                    LogValidationError(config.Id, ex.Message);
                    validationErrors.Add(new ConfigurationError(config.Id, ex.Message));
                } catch (InvalidOperationException ex) {
                    LogConfigurationError(config.Id, ex.Message);
                    validationErrors.Add(new ConfigurationError(config.Id, ex.Message));
                }
            }

            if (validationErrors.Count > 0) {
                return ConfigurationResult.CreateFailure(validationErrors);
            }

            var result = await _repository.SaveAsync(ConfigName, configsList).ConfigureAwait(false);
            if (!result.Success) {
                LogConfigurationSaveError(result.Error!);
                return ConfigurationResult.CreateFailure([
                    new ConfigurationError(string.Empty, $"Failed to save configurations: {result.Error}")
                ]);
            }

            _sensors.Clear();
            foreach (var (id, sensor) in sensorsDict)
                _sensors.TryAdd(id, sensor);

            LogConfigurationUpdateSuccess(sensorsDict.Count);
            return ConfigurationResult.CreateSuccess();
        } catch (Exception ex) {
            LogConfigurationUpdateError(ex.Message);
            return ConfigurationResult.CreateFailure([
                new ConfigurationError(string.Empty, $"Unexpected error: {ex.Message}")
            ]);
        }
    }

    private async Task LoadConfigurationAsync() {
        var result = await _repository.LoadAsync<List<SensorConfig>>(ConfigName).ConfigureAwait(false);

        if (!result.Success) {
            var defaultConfigs = CreateDefaultSensorConfigs();
            await _repository.SaveAsync(ConfigName, defaultConfigs).ConfigureAwait(false);

            foreach (var config in defaultConfigs) {
                var sensor = Sensor.FromConfig(config);
                _sensors.TryAdd(sensor.Id.Value, sensor);
            }

            LogDefaultConfigurationCreated(defaultConfigs.Count);
            return;
        }

        foreach (var config in result.Data!) {
            var sensor = Sensor.FromConfig(config);
            _sensors.TryAdd(sensor.Id.Value, sensor);
        }

        LogConfigurationLoadSuccess(result.Data!.Count);
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

    #region Logging

    [LoggerMessage(Level = LogLevel.Error, Message = "Validation error for sensor {sensorId}: {message}")]
    private partial void LogValidationError(string sensorId, string message);

    [LoggerMessage(Level = LogLevel.Error, Message = "Duplicate sensor IDs found: {Ids}")]
    private partial void LogValidationDuplicateIds(string ids);

    [LoggerMessage(Level = LogLevel.Error, Message = "Configuration error for sensor {sensorId}: {message}")]
    private partial void LogConfigurationError(string sensorId, string message);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to save sensor configurations: {message}")]
    private partial void LogConfigurationSaveError(string message);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to update sensor configurations: {message}")]
    private partial void LogConfigurationUpdateError(string message);

    [LoggerMessage(Level = LogLevel.Information, Message = "Successfully updated {count} sensor configurations")]
    private partial void LogConfigurationUpdateSuccess(int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Created {count} default sensor configurations")]
    private partial void LogDefaultConfigurationCreated(int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Successfully loaded {count} sensor configurations")]
    private partial void LogConfigurationLoadSuccess(int count);

    #endregion
}
