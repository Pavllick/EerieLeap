using EerieLeap.Configuration;
using EerieLeap.Hardware;

namespace EerieLeap.Services;

public interface IAdcConfigurationService {
    Task InitializeAdcAsync();
    Task<IAdc> GetAdcAsync();
    Task<AdcConfig> GetConfigurationAsync();
    Task UpdateConfigurationAsync(AdcConfig config);
}
