using EerieLeap.Configuration;
using EerieLeap.Domain.AdcDomain.Hardware;

namespace EerieLeap.Domain.AdcDomain.Services;

public interface IAdcConfigurationService : IDisposable {
    Task InitializeAsync();
    AdcConfig? GetConfiguration();
    IAdc? GetAdc();
    Task UpdateConfigurationAsync(AdcConfig config);
}
