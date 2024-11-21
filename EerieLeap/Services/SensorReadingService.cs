using EerieLeap.Hardware;
using EerieLeap.Aspects;
using EerieLeap.Configuration;
using EerieLeap.Types;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace EerieLeap.Services;

public sealed class SensorReadingService : BackgroundService, ISensorReadingService {
    private readonly ILogger<SensorReadingService> _logger;
    private readonly AdcFactory _adcFactory;
    private readonly string _configPath;
    private readonly object _lock = new();
    private List<SensorConfig> _sensorConfigs;
    private AdcConfig _adcConfig;
    private IAdc? _adc;
    private Dictionary<string, double> _lastReadings = new();
    private TaskCompletionSource<bool>? _initializationTcs;

    public SensorReadingService(
        ILogger<SensorReadingService> logger,
        AdcFactory adcFactory,
        IConfiguration configuration) {
        _logger = logger;
        _adcFactory = adcFactory;
        _configPath = configuration.GetValue<string>("ConfigurationPath") ?? throw new ArgumentException("ConfigurationPath not set in configuration");
        _sensorConfigs = new List<SensorConfig>();
        _adcConfig = new AdcConfig(); // Initialize with empty config
    }

    public async Task<IEnumerable<ReadingResult>> GetReadingsAsync() {
        return await Task.Run(() => {
            lock (_lock) {
                return _lastReadings.Select(kvp => new ReadingResult {
                    Id = kvp.Key,
                    Value = kvp.Value
                });
            }
        }).ConfigureAwait(false);
    }

    public async Task<ReadingResult?> GetReadingAsync(string id) {
        return await Task.Run<ReadingResult?>(() => {
            lock (_lock) {
                return _lastReadings.TryGetValue(id, out var value)
                    ? new ReadingResult { Id = id, Value = value }
                    : null;
            }
        }).ConfigureAwait(false);
    }

    public async Task<AdcConfig> GetAdcConfigurationAsync() {
        return await Task.Run(() => {
            lock (_lock) {
                return _adcConfig;
            }
        }).ConfigureAwait(false);
    }

    public async Task<IEnumerable<SensorConfig>> GetSensorConfigurationsAsync() {
        return await Task.Run(() => {
            lock (_lock) {
                return _sensorConfigs.AsEnumerable();
            }
        }).ConfigureAwait(false);
    }

    [Validate]
    public async Task UpdateAdcConfigurationAsync([Required] AdcConfig config) {
        await Task.Run(() => {
            lock (_lock) {
                _adcConfig = config;
                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(Path.Combine(_configPath, "adc.json"), json);
            }
        }).ConfigureAwait(false);

        await InitializeAdcAsync();
    }

    [Validate]
    public async Task UpdateSensorConfigurationsAsync([Required] IEnumerable<SensorConfig> configs) {
        await Task.Run(() => {
            lock (_lock) {
                _sensorConfigs = configs.ToList();
                _lastReadings.Clear(); // TODO: Should we clear readings only for changed sensors
                var json = JsonSerializer.Serialize(_sensorConfigs, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(Path.Combine(_configPath, "sensors.json"), json);
            }
        }).ConfigureAwait(false);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        _initializationTcs = new TaskCompletionSource<bool>();

        try {
            await LoadConfigs().ConfigureAwait(false);
            await InitializeAdcAsync().ConfigureAwait(false);
            _initializationTcs.TrySetResult(true);

            while (!stoppingToken.IsCancellationRequested) {
                try {
                    await UpdateReadingsAsync().ConfigureAwait(false);
                    await Task.Delay(1000, stoppingToken).ConfigureAwait(false);
                } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                    // Normal shutdown, no need to log error
                    break;
                } catch (Exception ex) {
                    _logger.LogError(ex, "Error updating readings");
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

    private async Task UpdateReadingsAsync() {
        Dictionary<string, double> newReadings = new();
        IAdc? adc;
        SensorConfig[] sensors;

        lock (_lock) {
            if (_adc == null || _sensorConfigs.Count == 0) {
                return;
            }

            adc = _adc;
            sensors = _sensorConfigs.ToArray();
        }

        // First process ADC sensors
        foreach (var sensor in sensors.Where(s => s.Type != SensorType.Virtual)) {
            try {
                if (sensor.Channel == null) {
                    _logger.LogError("Channel not specified for physical sensor {Name}", sensor.Name);
                    continue;
                }
                var voltage = await adc.ReadChannelAsync(sensor.Channel.Value);
                newReadings[sensor.Id] = ConvertVoltageToValue(voltage, sensor);
            } catch (Exception ex) {
                _logger.LogError(ex, "Failed to read ADC sensor {Name}", sensor.Name);
            }
        }

        // Then process virtual sensors
        foreach (var sensor in sensors.Where(s => s.Type == SensorType.Virtual)) {
            try {
                if (string.IsNullOrEmpty(sensor.ConversionExpression)) {
                    _logger.LogWarning("Virtual sensor {Name} has invalid configuration - missing expression", sensor.Name);
                    continue;
                }

                // Extract sensor IDs from expression and create value dictionary
                var sensorIds = ExpressionEvaluator.ExtractSensorIds(sensor.ConversionExpression);
                var sensorValues = sensorIds.ToDictionary(id => id,
                    id => newReadings.TryGetValue(id, out var value) ? value : 0.0);

                newReadings[sensor.Id] = ExpressionEvaluator.EvaluateWithSensors(
                    sensor.ConversionExpression,
                    sensorValues);
            } catch (Exception ex) {
                _logger.LogError(ex, "Failed to evaluate virtual sensor {Name}", sensor.Name);
            }
        }

        lock (_lock) {
            _lastReadings = newReadings;
        }
    }

    private async Task LoadConfigs() {
        try {
            var adcConfigPath = Path.Combine(_configPath, "adc.json");
            var sensorConfigPath = Path.Combine(_configPath, "sensors.json");

            if (!File.Exists(adcConfigPath) || !File.Exists(sensorConfigPath)) {
                CreateDefaultConfigs();
                return;
            }

            var options = new JsonSerializerOptions {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };

            var adcJson = await File.ReadAllTextAsync(adcConfigPath);
            var sensorsJson = await File.ReadAllTextAsync(sensorConfigPath);

            _adcConfig = JsonSerializer.Deserialize<AdcConfig>(adcJson, options) ?? throw new JsonException("Failed to deserialize ADC config");
            _sensorConfigs = JsonSerializer.Deserialize<List<SensorConfig>>(sensorsJson, options) ?? throw new JsonException("Failed to deserialize sensor configs");
        } catch (Exception ex) {
            _logger.LogError(ex, "Failed to load configurations");
            throw;
        }
    }

    private void CreateDefaultConfigs() {
        _adcConfig = CreateDefaultAdcConfig();
        _sensorConfigs = CreateDefaultSensorConfigs().ToList();

        var options = new JsonSerializerOptions {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        // Ensure config directory exists
        Directory.CreateDirectory(_configPath);

        File.WriteAllText(Path.Combine(_configPath, "adc.json"),
            JsonSerializer.Serialize(_adcConfig, options));
        File.WriteAllText(Path.Combine(_configPath, "sensors.json"),
            JsonSerializer.Serialize(_sensorConfigs, options));
    }

    private static AdcConfig CreateDefaultAdcConfig() {
        return new AdcConfig {
            Type = "ADS7953",
            BusId = 0,
            ChipSelect = 0,
            ClockFrequency = 1_000_000,
            Mode = 0,
            DataBitLength = 8,
            Resolution = 12,
            ReferenceVoltage = 3.3,
            Protocol = new AdcProtocolConfig {
                CommandPrefix = Convert.FromHexString("40"),
                ChannelBitShift = 2,
                ChannelMask = 15,
                ResultBitMask = 4095,
                ResultBitShift = 0,
                ReadByteCount = 2
            }
        };
    }

    private static SensorConfig[] CreateDefaultSensorConfigs() {
        return new SensorConfig[]
        {
            new() {
                Id = "coolant_temp",
                Name = "Coolant Temperature",
                Type = SensorType.Temperature,
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
                Type = SensorType.Temperature,
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
    }

    private Task InitializeAdcAsync() {
        try {
            lock (_lock) {
                _adc?.Dispose();
                _adc = _adcFactory.CreateAdc();
                _adc.Configure(_adcConfig);
            }
            return Task.CompletedTask;
        } catch (Exception ex) {
            _logger.LogError(ex, "Failed to initialize ADC");
            throw;
        }
    }

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
            !sensor.MinValue.HasValue || !sensor.MaxValue.HasValue)
            throw new InvalidOperationException(
                $"Sensor {sensor.Name} is missing required voltage or value range configuration");

        // Default linear conversion
        var voltageRange = sensor.MaxVoltage.Value - sensor.MinVoltage.Value;
        var valueRange = sensor.MaxValue.Value - sensor.MinValue.Value;

        return ((voltage - sensor.MinVoltage.Value) / voltageRange * valueRange) + sensor.MinValue.Value;
    }
}
