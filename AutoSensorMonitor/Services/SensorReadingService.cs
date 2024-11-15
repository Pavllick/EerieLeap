using AutoSensorMonitor.Service.Hardware;
using AutoSensorMonitor.Service.Models;
using AutoSensorMonitor.Service.Aspects;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace AutoSensorMonitor.Service.Services;

public sealed class SensorReadingService : BackgroundService, ISensorReadingService
{
    private readonly ILogger<SensorReadingService> _logger;
    private readonly AdcFactory _adcFactory;
    private readonly string _configPath;
    private readonly object _lock = new();
    private IAdc? _adc;
    private SystemConfig _config;
    private Dictionary<int, double> _lastReadings = new();

    public SensorReadingService(
        ILogger<SensorReadingService> logger, 
        AdcFactory adcFactory, 
        IConfiguration configuration) 
    {
        _logger = logger;
        _adcFactory = adcFactory;
        _configPath = configuration["ConfigPath"] ?? "config.json";
        _config = LoadConfig();
    }

    public async Task<Dictionary<string, double>> GetCurrentReadingsAsync() {
        return await Task.Run(() => {
            lock (_lock) {
                return _lastReadings.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value);
            }
        }).ConfigureAwait(false);
    }

    public async Task<SystemConfig> GetConfigurationAsync() {
        return await Task.Run(() => {
            lock (_lock) {
                return _config;
            }
        }).ConfigureAwait(false);
    }

    [Validate]
    public async Task UpdateConfigurationAsync([Required] SystemConfig config) {
        await Task.Run(() => {
            lock (_lock) {
                _config = config;
                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configPath, json);
                InitializeAdc();
            }
        }).ConfigureAwait(false);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        // Load configuration on startup
        lock (_lock) {
            _config = LoadConfig();
            InitializeAdc();
        }

        while (!stoppingToken.IsCancellationRequested) {
            try {
                await UpdateReadingsAsync(stoppingToken).ConfigureAwait(false);
                await Task.Delay(1000, stoppingToken).ConfigureAwait(false);
            } catch (Exception ex) when (ex is not OperationCanceledException) {
                _logger.LogError(ex, "Error updating sensor readings");
            }
        }
    }

    private async Task UpdateReadingsAsync(CancellationToken cancellationToken) {
        Dictionary<int, double> newReadings = new();
        IAdc? adc;
        List<SensorConfig> sensors;

        lock (_lock) {
            if (_adc == null || _config.Sensors.Count == 0) {
                return;
            }
            adc = _adc;
            sensors = _config.Sensors.ToList();
        }

        foreach (var sensor in sensors) {
            var voltage = await adc.ReadChannelAsync(sensor.Channel).ConfigureAwait(false);
            var value = ConvertVoltageToValue(voltage, sensor);
            newReadings[sensor.Channel] = value;
        }

        lock (_lock) {
            _lastReadings = newReadings;
        }
    }

    private static SystemConfig CreateDefaultConfig() {
        return new SystemConfig {
            AdcConfig = new AdcConfig {
                Type = "ADS7953",
                BusId = 0,
                ChipSelect = 0,
                ClockFrequency = 1_000_000
            },
            Sensors = new List<SensorConfig> {
                new() {
                    Name = "Coolant Temperature",
                    Channel = 0,
                    Type = SensorType.Temperature,
                    MinVoltage = 0.5,
                    MaxVoltage = 4.5,
                    MinValue = 0,
                    MaxValue = 120,
                    Unit = "°C",
                    SamplingRateMs = 1000
                },
                new() {
                    Name = "Oil Temperature",
                    Channel = 1,
                    Type = SensorType.Temperature,
                    MinVoltage = 0.5,
                    MaxVoltage = 4.5,
                    MinValue = 0,
                    MaxValue = 150,
                    Unit = "°C",
                    SamplingRateMs = 1000
                },
                new() {
                    Name = "Oil Pressure",
                    Channel = 2,
                    Type = SensorType.Pressure,
                    MinVoltage = 0.5,
                    MaxVoltage = 4.5,
                    MinValue = 0,
                    MaxValue = 10,
                    Unit = "bar",
                    SamplingRateMs = 500
                }
            }
        };
    }

    private SystemConfig LoadConfig() {
        try {
            if (!File.Exists(_configPath)) {
                var defaultConfig = CreateDefaultConfig();
                var json = JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configPath, json);
                return defaultConfig;
            }

            var configJson = File.ReadAllText(_configPath);
            var loadedConfig = JsonSerializer.Deserialize<SystemConfig>(configJson);

            return loadedConfig ?? _config;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to load configuration, using default config");
            return _config;
        }
    }

    private void InitializeAdc() {
        try {
            _adc = _adcFactory.CreateAdc(_config.AdcConfig.Type);
            _adc.Configure(_config.AdcConfig);
        } catch (Exception ex) {
            _logger.LogError(ex, "Failed to initialize ADC");
            throw;
        }
    }

    private static double ConvertVoltageToValue(double voltage, SensorConfig sensor) {
        var voltageRange = sensor.MaxVoltage - sensor.MinVoltage;
        var valueRange = sensor.MaxValue - sensor.MinValue;
        
        return ((voltage - sensor.MinVoltage) / voltageRange * valueRange) + sensor.MinValue;
    }

    public override void Dispose() {
        _adc?.Dispose();
        base.Dispose();
    }
}
