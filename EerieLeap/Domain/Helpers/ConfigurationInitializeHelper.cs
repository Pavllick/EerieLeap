using System.ComponentModel.DataAnnotations;
using EerieLeap.Configuration;
using Microsoft.Extensions.Options;

namespace EerieLeap.Domain.Helpers;
internal partial class ConfigurationInitializeHelper {
    private readonly ILogger _logger;
    private readonly Settings _settings;

    public ConfigurationInitializeHelper([Required] ILogger logger, [Required] IOptions<Settings> settings) {
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task TryInitialize(Func<CancellationToken, Task<bool>> action, string moduleName, CancellationToken stoppingToken) {
        try {
            if (!await action(stoppingToken).ConfigureAwait(false))
                ConfigurationInitializationError(moduleName);
            else
                return;
        } catch (Exception ex) {
            InitializationError(moduleName);
        }

        await Task.Delay(_settings.ConfigurationLoadRetryMs, stoppingToken).ConfigureAwait(false);

        try {
            while (!await action(stoppingToken).ConfigureAwait(false))
                await Task.Delay(_settings.ConfigurationLoadRetryMs, stoppingToken).ConfigureAwait(false);
        } catch { }
    }

    #region Loggers

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to initialize {moduleName}")]
    private partial void InitializationError(string moduleName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to load {moduleName}")]
    private partial void ConfigurationInitializationError(string moduleName);

    #endregion
}
