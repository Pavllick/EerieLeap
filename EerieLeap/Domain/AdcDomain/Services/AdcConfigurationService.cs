using EerieLeap.Configuration;
using EerieLeap.Domain.AdcDomain.Hardware;
using EerieLeap.Domain.Helpers;
using EerieLeap.Repositories;
using EerieLeap.Utilities;
using System.ComponentModel.DataAnnotations;

namespace EerieLeap.Domain.AdcDomain.Services;

internal sealed partial class AdcConfigurationService : IAdcConfigurationService {
    private readonly ILogger _logger;
    private readonly ConfigurationInitializeHelper _configurationInitializeHelper;
    private readonly IAdc _adc;
    private readonly IConfigurationRepository _repository;
    private readonly AsyncLock _asyncLock = new();

    private AdcConfig? _config;
    private string? _processingScript;

    public AdcConfigurationService(
        ILogger logger,
        [Required] ConfigurationInitializeHelper configurationInitializeHelper,
        [Required] IAdc adc,
        [Required] IConfigurationRepository repository) {

        _logger = logger;
        _configurationInitializeHelper = configurationInitializeHelper;
        _adc = adc;
        _repository = repository;
    }

    public async Task InitializeAsync(CancellationToken stoppingToken) {
        await _configurationInitializeHelper
            .TryInitialize(InitializeConfigurationAsync, "ADC configuration", stoppingToken)
            .ConfigureAwait(false);

        await _configurationInitializeHelper
            .TryInitialize(InitializeProcessingScriptAsync, "ADC processing script", stoppingToken)
            .ConfigureAwait(false);
    }

    private async Task<bool> InitializeConfigurationAsync(CancellationToken stoppingToken) {
        if (!await LoadConfigurationAsync().ConfigureAwait(false))
            return false;

        using var releaser = await _asyncLock.LockAsync(stoppingToken).ConfigureAwait(false);

        _adc.UpdateConfiguration(_config!);

        return true;
    }

    private async Task<bool> InitializeProcessingScriptAsync(CancellationToken stoppingToken) {
        if (!File.Exists(GetConfigurationScritpPath()))
            return false;

        string jsConfigScriptCode = await File.ReadAllTextAsync(GetConfigurationScritpPath(), stoppingToken).ConfigureAwait(false);
        _adc.UpdateProcessingScript(jsConfigScriptCode);

        _processingScript = jsConfigScriptCode;

        return true;
    }

    public IAdc? GetAdc() =>
        _adc;

    public AdcConfig? GetConfiguration() =>
        _config;

    public string? GetProcessingScript() =>
        _processingScript;

    public async Task UpdateConfigurationAsync([Required] AdcConfig config, CancellationToken stoppingToken) {
        using var releaser = await _asyncLock.LockAsync(stoppingToken).ConfigureAwait(false);

        var result = await _repository.SaveAsync(AppConstants.AdcConfigFileName, config).ConfigureAwait(false);
        if (!result.Success)
            throw new InvalidOperationException($"Failed to save ADC configuration: {string.Join(',', result.Errors.Select(e => e.Message))}");

        _adc.UpdateConfiguration(config);
        _config = config;

        LogConfigurationUpdated();
    }

    public async Task UpdateProcessingScriptAsync([Required] string processingScript, CancellationToken stoppingToken) {
        using var releaser = await _asyncLock.LockAsync(stoppingToken).ConfigureAwait(false);

        if (!File.Exists(AppConstants.ConfigDirPath))
            Directory.CreateDirectory(AppConstants.ConfigDirPath);

        _adc.UpdateProcessingScript(processingScript);
        _processingScript = processingScript;

        await File.WriteAllTextAsync(GetConfigurationScritpPath(), processingScript, stoppingToken).ConfigureAwait(false);

        LogProcessingScriptUpdated();
    }

    private async Task<bool> LoadConfigurationAsync() {
        var result = await _repository.LoadAsync<AdcConfig>(AppConstants.AdcConfigFileName).ConfigureAwait(false);

        _config = result.Data;

        return result.Success;
    }

    private static string GetConfigurationScritpPath() =>
        Path.Combine(AppConstants.ConfigDirPath, $"{AppConstants.AdcConfigScriptFileName}.js");

    public void Dispose() {
        _asyncLock.Dispose();
        _adc?.Dispose();
    }

    #region Loggers

    [LoggerMessage(Level = LogLevel.Information, Message = "Updated ADC configuration")]
    private partial void LogConfigurationUpdated();

    [LoggerMessage(Level = LogLevel.Information, Message = "Updated ADC processing script")]
    private partial void LogProcessingScriptUpdated();

    #endregion
}
