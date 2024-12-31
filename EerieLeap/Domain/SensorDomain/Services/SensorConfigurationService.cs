using System.Collections.Concurrent;
using EerieLeap.Types;
using System.ComponentModel.DataAnnotations;
using EerieLeap.Configuration;
using EerieLeap.Repositories;
using EerieLeap.Utilities;
using EerieLeap.Domain.SensorDomain.Models;

namespace EerieLeap.Domain.SensorDomain.Services;

internal partial class SensorConfigurationService : ISensorConfigurationService {
    private readonly ILogger _logger;
    private readonly IConfigurationRepository _repository;
    private readonly ConcurrentDictionary<string, Sensor> _sensors = new();
    private readonly AsyncLock _asyncLock = new();
    private bool _disposed;

    public SensorConfigurationService(ILogger logger, [Required] IConfigurationRepository repository) {
        _logger = logger;
        _repository = repository;
    }

    public async Task<bool> InitializeAsync() =>
        _sensors.IsEmpty
            ? await LoadConfigurationAsync().ConfigureAwait(false)
            : true;

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

                LogValidationDuplicateIds(string.Join(", ", duplicateIds));

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

            var result = await _repository.SaveAsync(AppConstants.SensorsConfigFileName, configsList).ConfigureAwait(false);
            if (!result.Success) {
                LogConfigurationSaveError(string.Join(',', result.Errors.Select(e => e.Message))!);
                return ConfigurationResult.CreateFailure([
                    new ConfigurationError(string.Empty, $"Failed to save configurations: {string.Join(',', result.Errors.Select(e => e.Message))}")
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

    private async Task<bool> LoadConfigurationAsync() {
        var result = await _repository.LoadAsync<List<SensorConfig>>(AppConstants.SensorsConfigFileName).ConfigureAwait(false);

        if (!result.Success) {
            await _repository.SaveAsync(AppConstants.SensorsConfigFileName, Array.Empty<SensorConfig>()).ConfigureAwait(false);
        } else {
            foreach (var config in result.Data!) {
                var sensor = Sensor.FromConfig(config);
                _sensors.TryAdd(sensor.Id.Value, sensor);
            }
        }

        LogConfigurationLoadSuccess(_sensors.Count);

        return true;
    }

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

    [LoggerMessage(Level = LogLevel.Information, Message = "Successfully loaded {count} sensor configurations")]
    private partial void LogConfigurationLoadSuccess(int count);

    #endregion
}
