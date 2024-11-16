using AutoSensorMonitor.Configuration;
using AutoSensorMonitor.Types;

namespace AutoSensorMonitor.Services;

public interface ISensorReadingService
{
    Task<IEnumerable<ReadingResult>> GetReadingsAsync();
    Task<ReadingResult?> GetReadingAsync(string id);
    Task<AdcConfig> GetAdcConfigurationAsync();
    Task<IEnumerable<SensorConfig>> GetSensorConfigurationsAsync();
    Task UpdateAdcConfigurationAsync(AdcConfig config);
    Task UpdateSensorConfigurationsAsync(IEnumerable<SensorConfig> configs);
}
