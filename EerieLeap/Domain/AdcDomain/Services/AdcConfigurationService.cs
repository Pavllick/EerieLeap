using EerieLeap.Configuration;
using EerieLeap.Domain.AdcDomain.Hardware;
using EerieLeap.Repositories;
using EerieLeap.Utilities;
using System.ComponentModel.DataAnnotations;

namespace EerieLeap.Domain.AdcDomain.Services;

internal sealed partial class AdcConfigurationService : IAdcConfigurationService {
    private readonly ILogger _logger;
    private readonly AdcFactory _adcFactory;
    private readonly IConfigurationRepository _repository;
    private readonly AsyncLock _asyncLock = new();

    private AdcConfig? _config;
    private IAdc? _adc;

    public AdcConfigurationService(ILogger logger, [Required] AdcFactory adcFactory, [Required] IConfigurationRepository repository) {
        _logger = logger;
        _adcFactory = adcFactory;
        _repository = repository;
    }

    public async Task<bool> InitializeAsync() {
        if (_adc != null)
            return true;

        if(!await LoadConfigurationAsync().ConfigureAwait(false))
            return false;

        using var releaser = await _asyncLock.LockAsync().ConfigureAwait(false);

        _adc = _adcFactory.CreateAdc();
        _adc.Configure(_config!);

        return true;
    }

    public IAdc? GetAdc() =>
        _adc;

    public AdcConfig? GetConfiguration() =>
        _config;

    public async Task UpdateConfigurationAsync([Required] AdcConfig config) {
        using var releaser = await _asyncLock.LockAsync().ConfigureAwait(false);

        var result = await _repository.SaveAsync(AppConstants.AdcConfigFileName, config).ConfigureAwait(false);
        if (!result.Success)
            throw new InvalidOperationException($"Failed to save ADC configuration: {string.Join(',', result.Errors.Select(e => e.Message))}");

        _config = config;
        _adc?.Configure(config);

        LogConfigurationUpdated();
    }

    private async Task<bool> LoadConfigurationAsync() {
        var result = await _repository.LoadAsync<AdcConfig>(AppConstants.AdcConfigFileName).ConfigureAwait(false);

        _config = result.Data;

        return result.Success;
    }

    public void Dispose() {
        _asyncLock.Dispose();
        _adc?.Dispose();
    }

    #region Loggers

    [LoggerMessage(Level = LogLevel.Information, Message = "Updated ADC configuration")]
    private partial void LogConfigurationUpdated();

    #endregion
}
