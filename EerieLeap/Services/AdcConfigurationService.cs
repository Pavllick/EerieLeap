using EerieLeap.Configuration;
using EerieLeap.Hardware;
using EerieLeap.Repositories;
using EerieLeap.Utilities;
using System.ComponentModel.DataAnnotations;

namespace EerieLeap.Services;

public sealed partial class AdcConfigurationService : IAdcConfigurationService {
    private readonly ILogger _logger;
    private readonly AdcFactory _adcFactory;
    private readonly IConfigurationRepository _repository;
    private readonly AsyncLock _asyncLock = new();
    private AdcConfig? _config;
    private IAdc? _adc;

    private const string ConfigName = "adc";

    public AdcConfigurationService(ILogger logger, AdcFactory adcFactory, IConfigurationRepository repository) {
        _logger = logger;
        _adcFactory = adcFactory;
        _repository = repository;
    }

    public async Task InitializeAsync() {
        if (_adc != null)
            return;

        await LoadConfigurationAsync().ConfigureAwait(false);

        using var releaser = await _asyncLock.LockAsync().ConfigureAwait(false);

        _adc = _adcFactory.CreateAdc();
        _adc.Configure(_config!);
    }

    public IAdc? GetAdc() =>
        _adc;

    public AdcConfig? GetConfiguration() =>
        _config;

    public async Task UpdateConfigurationAsync([Required] AdcConfig config) {
        using var releaser = await _asyncLock.LockAsync().ConfigureAwait(false);

        var result = await _repository.SaveAsync(ConfigName, config).ConfigureAwait(false);
        if (!result.Success)
            throw new InvalidOperationException($"Failed to save ADC configuration: {result.Error}");

        _config = config;
        _adc?.Configure(config);
    }

    private async Task LoadConfigurationAsync() {
        var result = await _repository.LoadAsync<AdcConfig>(ConfigName);

        if (!result.Success) {
            _config = CreateDefaultConfiguration();
            await _repository.SaveAsync(ConfigName, _config);
            return;
        }

        _config = result.Data;
    }

    private static AdcConfig CreateDefaultConfiguration() =>
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
