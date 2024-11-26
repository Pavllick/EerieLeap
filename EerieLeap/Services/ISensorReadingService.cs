using EerieLeap.Configuration;
using EerieLeap.Types;

namespace EerieLeap.Services;

public interface ISensorReadingService {
    Task<IEnumerable<ReadingResult>> GetReadingsAsync();
    Task<ReadingResult?> GetReadingAsync(string id);
    Task<IEnumerable<SensorConfig>> GetSensorConfigurationsAsync();
    Task UpdateConfigurationAsync(IEnumerable<SensorConfig> configs);
    Task WaitForInitializationAsync();
}
