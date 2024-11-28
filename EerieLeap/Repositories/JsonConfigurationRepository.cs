using System.Text.Json;
using EerieLeap.Utilities;

namespace EerieLeap.Repositories;

public partial class JsonConfigurationRepository : IConfigurationRepository {
    private readonly string _basePath;
    private readonly ILogger _logger;
    private readonly JsonSerializerOptions _readOptions;
    private readonly JsonSerializerOptions _writeOptions;
    private readonly AsyncLock _asyncLock = new();
    private bool _disposed;

    public JsonConfigurationRepository(
        ILogger logger,
        IConfiguration configuration,
        JsonSerializerOptions? readOptions = null,
        JsonSerializerOptions? writeOptions = null) {

        _logger = logger;
        _basePath = configuration.GetValue<string>("ConfigurationPath")
            ?? throw new ArgumentException("ConfigurationPath not set in configuration");

        _readOptions = readOptions ?? new JsonSerializerOptions {
            PropertyNameCaseInsensitive = true
        };

        _writeOptions = writeOptions ?? new JsonSerializerOptions {
            WriteIndented = true
        };

        // Ensure configuration directory exists
        Directory.CreateDirectory(_basePath);
    }

    public async Task<ConfigurationResult<T>> LoadAsync<T>(string name) where T : class {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try {
            using var releaser = await _asyncLock.LockAsync().ConfigureAwait(false);
            var path = GetConfigPath(name);

            if (!File.Exists(path))
                return new ConfigurationResult<T>(false, Error: $"Configuration '{name}' not found");

            var json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
            Console.WriteLine(json);

            var config = JsonSerializer.Deserialize<T>(json, _readOptions);

            if (config == null)
                return new ConfigurationResult<T>(false, Error: $"Failed to deserialize configuration '{name}'");

            LogConfigurationLoaded(name);
            return new ConfigurationResult<T>(true, config);
        } catch (Exception ex) {
            LogConfigurationLoadError(name, ex);
            throw;
        }
    }

    public async Task<ConfigurationResult<T>> SaveAsync<T>(string name, T config) where T : class {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try {
            using var releaser = await _asyncLock.LockAsync().ConfigureAwait(false);
            var path = GetConfigPath(name);

            var json = JsonSerializer.Serialize(config, _writeOptions);
            await File.WriteAllTextAsync(path, json).ConfigureAwait(false);

            LogConfigurationSaved(name);
            return new ConfigurationResult<T>(true, config);
        } catch (Exception ex) {
            LogConfigurationSaveError(name, ex);
            return new ConfigurationResult<T>(false, Error: $"Error saving configuration '{name}': {ex.Message}");
        }
    }

    public async Task<bool> ExistsAsync(string name) {
        ObjectDisposedException.ThrowIf(_disposed, this);

        using var releaser = await _asyncLock.LockAsync().ConfigureAwait(false);
        return File.Exists(GetConfigPath(name));
    }

    public async Task<bool> DeleteAsync(string name) {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try {
            using var releaser = await _asyncLock.LockAsync().ConfigureAwait(false);
            var path = GetConfigPath(name);

            if (!File.Exists(path))
                return false;

            File.Delete(path);
            LogConfigurationDeleted(name);
            return true;
        } catch (Exception ex) {
            LogConfigurationDeleteError(name, ex);
            return false;
        }
    }

    private string GetConfigPath(string name) =>
        Path.Combine(_basePath, $"{name}.json");

    public void Dispose() {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) {
        if (!_disposed) {
            if (disposing) {
                _asyncLock.Dispose();
            }
            _disposed = true;
        }
    }

    #region Logging

    [LoggerMessage(Level = LogLevel.Information, Message = "Configuration '{Name}' loaded successfully")]
    private partial void LogConfigurationLoaded(string name);

    [LoggerMessage(Level = LogLevel.Information, Message = "Configuration '{Name}' saved successfully")]
    private partial void LogConfigurationSaved(string name);

    [LoggerMessage(Level = LogLevel.Information, Message = "Configuration '{Name}' deleted successfully")]
    private partial void LogConfigurationDeleted(string name);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error loading configuration '{Name}'")]
    private partial void LogConfigurationLoadError(string name, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error saving configuration '{Name}'")]
    private partial void LogConfigurationSaveError(string name, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error deleting configuration '{Name}'")]
    private partial void LogConfigurationDeleteError(string name, Exception ex);

    #endregion
}
