using AutoSensorMonitor.Service.Hardware;
using AutoSensorMonitor.Service.Aspects;
using AutoSensorMonitor.Configuration;
using AutoSensorMonitor.Types;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    private AdcConfig _adcConfig;
    private List<SensorConfig> _sensorConfigs;
    private Dictionary<int, double> _lastReadings = new();

    public SensorReadingService(
        ILogger<SensorReadingService> logger, 
        AdcFactory adcFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _adcFactory = adcFactory;
        _configPath = configuration.GetValue<string>("ConfigurationPath") ?? throw new ArgumentException("ConfigurationPath not set in configuration");
        _sensorConfigs = new List<SensorConfig>();
        _adcConfig = new AdcConfig(); // Initialize with empty config
        LoadConfigs();
    }

    public async Task<Dictionary<string, double>> GetReadingsAsync()
    {
        return await Task.Run(() => {
            lock (_lock) {
                return _lastReadings.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value);
            }
        }).ConfigureAwait(false);
    }

    public async Task<AdcConfig> GetAdcConfigurationAsync()
    {
        return await Task.Run(() => {
            lock (_lock) {
                return _adcConfig;
            }
        }).ConfigureAwait(false);
    }

    public async Task<List<SensorConfig>> GetSensorConfigurationsAsync()
    {
        return await Task.Run(() => {
            lock (_lock) {
                return _sensorConfigs.ToList();
            }
        }).ConfigureAwait(false);
    }

    [Validate]
    public async Task UpdateAdcConfigurationAsync([Required] AdcConfig config)
    {
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
    public async Task UpdateSensorConfigurationsAsync([Required] List<SensorConfig> configs)
    {
        await Task.Run(() => {
            lock (_lock) {
                _sensorConfigs = configs;
                var json = JsonSerializer.Serialize(configs, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(Path.Combine(_configPath, "sensors.json"), json);
            }
        }).ConfigureAwait(false);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await UpdateReadingsAsync(stoppingToken);
            await Task.Delay(100, stoppingToken);
        }
    }

    private async Task UpdateReadingsAsync(CancellationToken cancellationToken)
    {
        Dictionary<int, double> newReadings = new();
        IAdc? adc;
        List<SensorConfig> sensors;

        lock (_lock) {
            if (_adc == null || _sensorConfigs.Count == 0) {
                return;
            }

            adc = _adc;
            sensors = _sensorConfigs.ToList();
        }

        foreach (var sensor in sensors)
        {
            var voltage = await adc.ReadChannelAsync(sensor.Channel, cancellationToken);
            newReadings[sensor.Channel] = ConvertVoltageToValue(voltage, sensor);
        }

        lock (_lock) {
            _lastReadings = newReadings;
        }
    }

    private void LoadConfigs()
    {
        try
        {
            var adcConfigPath = Path.Combine(_configPath, "adc.json");
            var sensorConfigPath = Path.Combine(_configPath, "sensors.json");

            if (!File.Exists(adcConfigPath) || !File.Exists(sensorConfigPath))
            {
                CreateDefaultConfigs();
            }

            var adcJson = File.ReadAllText(adcConfigPath);
            var sensorsJson = File.ReadAllText(sensorConfigPath);

            var options = new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } };
            _adcConfig = JsonSerializer.Deserialize<AdcConfig>(adcJson, options) ?? CreateDefaultAdcConfig();
            _sensorConfigs = JsonSerializer.Deserialize<List<SensorConfig>>(sensorsJson, options) ?? CreateDefaultSensorConfigs();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configurations, using defaults");
            CreateDefaultConfigs();
        }
    }

    private void CreateDefaultConfigs()
    {
        _adcConfig = CreateDefaultAdcConfig();
        _sensorConfigs = CreateDefaultSensorConfigs();

        var options = new JsonSerializerOptions { 
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };
        File.WriteAllText(Path.Combine(_configPath, "adc.json"), 
            JsonSerializer.Serialize(_adcConfig, options));
        File.WriteAllText(Path.Combine(_configPath, "sensors.json"), 
            JsonSerializer.Serialize(_sensorConfigs, options));
    }

    private static AdcConfig CreateDefaultAdcConfig()
    {
        return new AdcConfig
        {
            Type = "ADS7953",
            BusId = 0,
            ChipSelect = 0,
            ClockFrequency = 1_000_000,
            Mode = 0,
            DataBitLength = 8,
            Resolution = 12,
            ReferenceVoltage = 3.3,
            Protocol = new AdcProtocol
            {
                CommandPrefix = Convert.FromHexString("40"),
                ChannelBitShift = 2,
                ChannelMask = 15,
                ResultBitMask = 4095,
                ResultBitShift = 0,
                ReadByteCount = 2
            }
        };
    }

    private static List<SensorConfig> CreateDefaultSensorConfigs()
    {
        return new List<SensorConfig>
        {
            new()
            {
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
            new()
            {
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
            new()
            {
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
        };
    }

    private Task InitializeAdcAsync()
    {
        try
        {
            _adc?.Dispose();
            _adc = _adcFactory.CreateAdc();
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize ADC");
            throw;
        }
    }

    private static double ConvertVoltageToValue(double voltage, SensorConfig sensor)
    {
        var voltageRange = sensor.MaxVoltage - sensor.MinVoltage;
        var valueRange = sensor.MaxValue - sensor.MinValue;
        
        return ((voltage - sensor.MinVoltage) / voltageRange * valueRange) + sensor.MinValue;
    }
}
