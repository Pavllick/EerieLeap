using EerieLeap.Configuration;
using EerieLeap.Hardware;
using EerieLeap.Utilities;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace EerieLeap.Services;

public sealed partial class AdcConfigurationService : IAdcConfigurationService {
    private readonly ILogger _logger;
    private readonly AdcFactory _adcFactory;
    private readonly string _configPath;
    private readonly AsyncLock _asyncLock = new();
    private readonly JsonSerializerOptions _writeOptions = new() { WriteIndented = true };
    private readonly JsonSerializerOptions _readOptions = new() {
        PropertyNameCaseInsensitive = true
    };

    private AdcConfig _config;
    private IAdc? _adc;

    public AdcConfigurationService(ILogger logger, AdcFactory adcFactory, IConfiguration configuration) {
        _logger = logger;
        _adcFactory = adcFactory;
        _configPath = configuration.GetValue<string>("ConfigurationPath") ?? throw new ArgumentException("ConfigurationPath not set in configuration");
        _config = new AdcConfig();
    }

    public async Task InitializeAsync() {
        if (_adc != null)
            return;

        await LoadConfigurationAsync().ConfigureAwait(false);

        using var releaser = await _asyncLock.LockAsync().ConfigureAwait(false);

        _adc = _adcFactory.CreateAdc();
        _adc.Configure(_config);
    }

    public async Task<IAdc> GetAdcAsync() {
        using var releaser = await _asyncLock.LockAsync().ConfigureAwait(false);

        return _adc!;
    }

    public AdcConfig GetConfiguration() =>
        _config;

    public async Task UpdateConfigurationAsync([Required] AdcConfig config) {
        using var releaser = await _asyncLock.LockAsync().ConfigureAwait(false);

        var configPath = Path.Combine(_configPath, "adc.json");
        var json = JsonSerializer.Serialize(config, _writeOptions);
        await File.WriteAllTextAsync(configPath, json).ConfigureAwait(false);

        _config = config;

        LogConfigurationUpdated();
    }

    private async Task LoadConfigurationAsync() {
        using var releaser = await _asyncLock.LockAsync().ConfigureAwait(false);

        try {
            var configPath = Path.Combine(_configPath, "adc.json");

            AdcConfig config;

            if (File.Exists(configPath)) {
                var json = await File.ReadAllTextAsync(configPath).ConfigureAwait(false);
                config = JsonSerializer.Deserialize<AdcConfig>(json, _readOptions) ?? throw new JsonException("Failed to deserialize ADC config");
            } else {
                config = CreateDefaultConfig();

                Directory.CreateDirectory(_configPath);
                var json = JsonSerializer.Serialize(config, _writeOptions);
                await File.WriteAllTextAsync(configPath, json).ConfigureAwait(false);

                LogDefaultConfigurationCreated();
            }

            _config = config;
        } catch (Exception ex) {
            LogConfigurationLoadError(ex);
            throw;
        }
    }

    private static AdcConfig CreateDefaultConfig() =>
        new AdcConfig {
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

    public void Dispose() {
        _asyncLock.Dispose();
        _adc?.Dispose();
    }

    #region Loggers

    [LoggerMessage(Level = LogLevel.Error, EventId = 1, Message = "Failed to load ADC configuration")]
    private partial void LogConfigurationLoadError(Exception ex);

    [LoggerMessage(Level = LogLevel.Information, EventId = 2, Message = "Created default ADC configuration")]
    private partial void LogDefaultConfigurationCreated();

    [LoggerMessage(Level = LogLevel.Information, EventId = 3, Message = "Updated ADC configuration")]
    private partial void LogConfigurationUpdated();

    #endregion
}
