using EerieLeap.Configuration;
using EerieLeap.Hardware;

namespace EerieLeap.Services;

public interface IAdcConfigurationService : IDisposable {
    Task InitializeAsync();
    Task<IAdc> GetAdcAsync();
    AdcConfig GetConfiguration();
    Task UpdateConfigurationAsync(AdcConfig config);
}
