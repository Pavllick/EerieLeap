using System.ComponentModel.DataAnnotations;
using EerieLeap.Configuration;
using EerieLeap.Domain.AdcDomain.Hardware;

namespace EerieLeap.Domain.AdcDomain.Services;

public interface IAdcConfigurationService : IDisposable {
    Task InitializeAsync(CancellationToken stoppingToken);
    AdcConfig? GetConfiguration();
    IAdc? GetAdc();
    string? GetProcessingScript();
    Task UpdateConfigurationAsync(AdcConfig config, CancellationToken stoppingToken);
    Task UpdateProcessingScriptAsync([Required] string processingScript, CancellationToken stoppingToken);
}
