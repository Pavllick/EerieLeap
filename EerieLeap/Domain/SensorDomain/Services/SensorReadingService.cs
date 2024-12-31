using System.ComponentModel.DataAnnotations;
using EerieLeap.Utilities;
using EerieLeap.Domain.SensorDomain.Models;
using EerieLeap.Domain.SensorDomain.Processing;
using EerieLeap.Domain.AdcDomain.Services;
using EerieLeap.Domain.SensorDomain.Utilities;
using EerieLeap.Configuration;
using Microsoft.Extensions.Options;

namespace EerieLeap.Domain.SensorDomain.Services;

internal sealed partial class SensorReadingService : BackgroundService, ISensorReadingService {
    private readonly ILogger _logger;
    private readonly Settings _settings;
    private readonly IAdcConfigurationService _adcService;
    private readonly ISensorConfigurationService _sensorsConfigService;
    private readonly ISensorReadingProcessor _readingProcessor;
    private readonly AsyncLock _asyncLock = new();
    private readonly SensorReadingBuffer _buffer;
    private TaskCompletionSource<bool>? _initializationTcs;

    public SensorReadingService(
        ILogger logger,
        [Required] IOptions<Settings> settings,
        [Required] IAdcConfigurationService adcService,
        [Required] ISensorConfigurationService sensorsConfigService,
        [Required] ISensorReadingProcessor readingProcessor,
        [Required] SensorReadingBuffer buffer) {

        _logger = logger;
        _settings = settings.Value;
        _adcService = adcService;
        _sensorsConfigService = sensorsConfigService;
        _readingProcessor = readingProcessor;
        _buffer = buffer;
    }

    public async Task<IEnumerable<SensorReading>> GetReadingsAsync() {
        using var releaser = await _asyncLock.LockAsync().ConfigureAwait(false);

        return _buffer.GetAllReadings();
    }

    public async Task<SensorReading?> GetReadingAsync([Required] string id) {
        using var releaser = await _asyncLock.LockAsync().ConfigureAwait(false);

        return _buffer.GetReading(id);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        _initializationTcs = new TaskCompletionSource<bool>();

        try {
            await TryInitialize(_adcService.InitializeAsync, "ADC").ConfigureAwait(false);
            await TryInitialize(_sensorsConfigService.InitializeAsync, "Sensors").ConfigureAwait(false);

            _initializationTcs.TrySetResult(true);

            while (!stoppingToken.IsCancellationRequested) {
                try {
                    await ProcessSensorsAsync().ConfigureAwait(false);
                    await Task.Delay(_settings.ProcessSensorsIntervalMs, stoppingToken).ConfigureAwait(false);
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

        async Task TryInitialize(Func<Task<bool>> action, string moduleName) {
            try {
                if (!await action().ConfigureAwait(false))
                    ConfigurationInitializationError(moduleName);
                else
                    return;
            } catch (Exception ex) {
                InitializationError(moduleName);
            }

            await Task.Delay(_settings.ConfigurationLoadRetryMs, stoppingToken).ConfigureAwait(false);

            try {
                while (!await action().ConfigureAwait(false))
                    await Task.Delay(_settings.ConfigurationLoadRetryMs, stoppingToken).ConfigureAwait(false);
            } catch { }
        }
    }

    private async Task ProcessSensorsAsync() {
        using var releaser = await _asyncLock.LockAsync().ConfigureAwait(false);

        var configs = _sensorsConfigService.GetConfigurations()!;
        var sensors = configs.Select(Sensor.FromConfig).ToList();

        // Build dependency graph
        var dependencyResolver = new SensorDependencyResolver();
        foreach (var sensor in sensors)
            dependencyResolver.AddSensor(sensor);

        _buffer.Clear();

        // Process sensors in dependency order
        foreach (var sensor in dependencyResolver.GetProcessingOrder()) {
            try {
                // Create initial reading with configuration
                var reading = new SensorReading(
                    sensor,
                    0.0, // Initial value will be updated by processor
                    DateTime.UtcNow);

                // Process reading based on sensor type
                await _readingProcessor.ProcessReadingAsync(reading).ConfigureAwait(false);
            } catch (Exception ex) {
                LogReadingError(sensor.Id.Value, ex);
            }
        }
    }

    // Needed for unit tests to wait for initialization
    public Task WaitForInitializationAsync() =>
        _initializationTcs?.Task ?? Task.CompletedTask;

    public sealed override void Dispose() {
        _asyncLock.Dispose();
        base.Dispose();
    }

    #region Loggers
    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to initialize {moduleName}")]
    private partial void InitializationError(string moduleName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to load {moduleName} configuration")]
    private partial void ConfigurationInitializationError(string moduleName);

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

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to process reading for sensor {name}")]
    private partial void LogReadingError(string name, Exception ex);

    #endregion
}
