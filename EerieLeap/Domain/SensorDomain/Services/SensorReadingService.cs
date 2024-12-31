using System.ComponentModel.DataAnnotations;
using EerieLeap.Utilities;
using EerieLeap.Domain.SensorDomain.Models;
using EerieLeap.Domain.SensorDomain.Processing;
using EerieLeap.Domain.AdcDomain.Services;
using EerieLeap.Domain.SensorDomain.Utilities;

namespace EerieLeap.Domain.SensorDomain.Services;

internal sealed partial class SensorReadingService : BackgroundService, ISensorReadingService {
    private readonly ILogger _logger;
    private readonly IAdcConfigurationService _adcService;
    private readonly ISensorConfigurationService _sensorsConfigService;
    private readonly ISensorReadingProcessor _readingProcessor;
    private readonly AsyncLock _asyncLock = new();
    private readonly SensorReadingBuffer _buffer;
    private TaskCompletionSource<bool>? _initializationTcs;

    public SensorReadingService(
        ILogger logger,
        [Required] IAdcConfigurationService adcService,
        [Required] ISensorConfigurationService sensorsConfigService,
        [Required] ISensorReadingProcessor readingProcessor,
        [Required] SensorReadingBuffer buffer) {

        _logger = logger;
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
            await _adcService.InitializeAsync().ConfigureAwait(false);
            await _sensorsConfigService.InitializeAsync().ConfigureAwait(false);

            _initializationTcs.TrySetResult(true);

            while (!stoppingToken.IsCancellationRequested) {
                try {
                    await ProcessSensorsAsync().ConfigureAwait(false);
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
