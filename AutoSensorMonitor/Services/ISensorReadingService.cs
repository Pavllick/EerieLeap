using AutoSensorMonitor.Configuration;

namespace AutoSensorMonitor.Services;

public interface ISensorReadingService
{
    Task<Dictionary<string, double>> GetReadingsAsync();
    Task<AdcConfig> GetAdcConfigurationAsync();
    Task<List<SensorConfig>> GetSensorConfigurationsAsync();
    Task UpdateAdcConfigurationAsync(AdcConfig config);
    Task UpdateSensorConfigurationsAsync(List<SensorConfig> configs);
}
