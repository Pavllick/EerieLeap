using EerieLeap.Configuration;
using EerieLeap.Hardware;

namespace EerieLeap.Services;

public interface IAdcConfigurationService : IDisposable {
    Task InitializeAsync();
    AdcConfig? GetConfiguration();
    IAdc? GetAdc();
    Task UpdateConfigurationAsync(AdcConfig config);
}
