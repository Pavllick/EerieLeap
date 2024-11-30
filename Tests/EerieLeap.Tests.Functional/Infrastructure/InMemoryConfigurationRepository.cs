using EerieLeap.Repositories;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace EerieLeap.Tests.Functional.Infrastructure;

public partial class InMemoryConfigurationRepository : IConfigurationRepository, IDisposable {
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, string> _configurations;
    private readonly JsonSerializerOptions _readOptions;
    private readonly JsonSerializerOptions _writeOptions;
    private bool _disposed;

    public InMemoryConfigurationRepository(
        ILogger logger,
        JsonSerializerOptions? readOptions = null,
        JsonSerializerOptions? writeOptions = null) {
        _logger = logger;
        _configurations = new ConcurrentDictionary<string, string>();

        _readOptions = readOptions ?? new JsonSerializerOptions {
            PropertyNameCaseInsensitive = true
        };

        _writeOptions = writeOptions ?? new JsonSerializerOptions {
            WriteIndented = true
        };
    }

    public Task<bool> ExistsAsync(string name) =>
        Task.FromResult(_configurations.ContainsKey(name));

    public Task<ConfigurationResult<T>> LoadAsync<T>(string name) where T : class {
        try {
            if (!_configurations.TryGetValue(name, out var json)) {
                LogConfigNotFound(name);
                return Task.FromResult(new ConfigurationResult<T>(false, Error: $"Configuration '{name}' not found"));
            }

            var config = JsonSerializer.Deserialize<T>(json, _readOptions);
            if (config == null) {
                LogDeserializationFailed(name);
                return Task.FromResult(new ConfigurationResult<T>(false, Error: $"Failed to deserialize configuration '{name}'"));
            }

            return Task.FromResult(new ConfigurationResult<T>(true, config));
        } catch (Exception ex) {
            LogLoadError(name, ex);
            return Task.FromResult(new ConfigurationResult<T>(false, Error: ex.Message));
        }
    }

    public Task<ConfigurationResult<T>> SaveAsync<T>(string name, T config) where T : class {
        try {
            var json = JsonSerializer.Serialize(config, _writeOptions);
            _configurations[name] = json;
            LogConfigSaved(name);
            return Task.FromResult(new ConfigurationResult<T>(true, config));
        } catch (Exception ex) {
            LogSaveError(name, ex);
            return Task.FromResult(new ConfigurationResult<T>(false, Error: ex.Message));
        }
    }

    public Task<bool> DeleteAsync(string name) =>
        Task.FromResult(_configurations.TryRemove(name, out _));

    protected virtual void Dispose(bool disposing) {
        if (!_disposed) {
            if (disposing) {
                _configurations.Clear();
            }
            _disposed = true;
        }
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #region Loggers
    [LoggerMessage(Level = LogLevel.Warning, Message = "Configuration '{name}' not found")]
    private partial void LogConfigNotFound(string name);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to deserialize configuration '{name}'")]
    private partial void LogDeserializationFailed(string name);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error loading configuration '{name}'")]
    private partial void LogLoadError(string name, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Saved configuration '{name}'")]
    private partial void LogConfigSaved(string name);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error saving configuration '{name}'")]
    private partial void LogSaveError(string name, Exception ex);
    #endregion
}
