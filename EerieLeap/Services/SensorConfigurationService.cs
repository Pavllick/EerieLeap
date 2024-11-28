using System.Collections.Concurrent;
using EerieLeap.Types;
using System.ComponentModel.DataAnnotations;
using EerieLeap.Configuration;
using EerieLeap.Repositories;
using EerieLeap.Utilities;

namespace EerieLeap.Services;

public partial class SensorConfigurationService : ISensorConfigurationService {
    private readonly ConcurrentDictionary<string, SensorConfig> _configs = new();
    private readonly IConfigurationRepository _repository;
    private readonly ILogger _logger;
    private readonly AsyncLock _asyncLock = new();
    private bool _disposed;

    private const string ConfigName = "sensors";

    public SensorConfigurationService(ILogger logger, [Required] IConfigurationRepository repository) {
        _logger = logger;
        _repository = repository;
    }

    public async Task InitializeAsync() =>
        await LoadConfigurationAsync().ConfigureAwait(false);

    private async Task LoadConfigurationAsync() {
        var result = await _repository.LoadAsync<List<SensorConfig>>(ConfigName).ConfigureAwait(false);

        if (!result.Success) {
            var defaultConfigs = CreateDefaultSensorConfigs();

            await _repository.SaveAsync(ConfigName, defaultConfigs).ConfigureAwait(false);

            foreach (var config in defaultConfigs)
                _configs.TryAdd(config.Id, config);

            return;
        }

        foreach (var config in result.Data!)
            _configs.TryAdd(config.Id, config);
    }

    public IReadOnlyList<SensorConfig> GetConfigurations() {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _configs.Values.ToList().AsReadOnly();
    }

    public SensorConfig? GetConfiguration([Required] string sensorId) {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _configs.TryGetValue(sensorId, out var config) ? config : null;
    }

    public async Task<bool> UpdateConfigurationAsync([Required] IEnumerable<SensorConfig> configs) {
        using var releaser = await _asyncLock.LockAsync().ConfigureAwait(false);
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!ValidateConfigurations(configs))
            return false;

        // _lastReadings.Clear(); // TODO: Should we clear readings only for changed sensors

        var configsList = configs.ToList();
        var configsDict = configsList.ToDictionary(c => c.Id);

        var result = await _repository.SaveAsync(ConfigName, configsDict).ConfigureAwait(false);
        if (!result.Success) {
            LogConfigurationUpdateError(result.Error!);
            return false;
        }

        _configs.Clear();
        foreach (var (id, config) in configsDict)
            _configs.TryAdd(id, config);

        return true;
    }

    public bool ValidateConfigurations([Required] IEnumerable<SensorConfig> configs) {
        var configsList = configs.ToList();

        var duplicateIds = configsList
            .GroupBy(c => c.Id)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateIds.Count > 0) {
            LogValidationDuplicateIds(string.Join(", ", duplicateIds));
            return false;
        }

        foreach (var config in configsList) {
            if (!ValidateConfiguration(config))
                return false;

            if (config.Type == SensorType.Physical && configsList.Count(c => c.Channel == config.Channel) > 1) {
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

    [LoggerMessage(Level = LogLevel.Error, Message = "Configuration update failed: {Error}")]
    private partial void LogConfigurationUpdateError(string error);

    [LoggerMessage(Level = LogLevel.Error, Message = "Validation error: {Message}")]
    private partial void LogValidationError(string message);

    [LoggerMessage(Level = LogLevel.Error, Message = "Duplicate sensor IDs found: {Ids}")]
    private partial void LogValidationDuplicateIds(string ids);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Validation failed: duplicate channel {channel} found for physical sensor {sensorId}")]
    private partial void LogValidationDuplicateChannel(int? channel, string sensorId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Validation error for sensor {sensorId}: {error}")]
    private partial void LogValidationError(string sensorId, string error);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Validation failed for sensor {sensorId}: MinVoltage ({minVoltage}) must be less than MaxVoltage ({maxVoltage})")]
    private partial void LogValidationVoltageError(string sensorId, double? minVoltage, double? maxVoltage);

    #endregion
}
