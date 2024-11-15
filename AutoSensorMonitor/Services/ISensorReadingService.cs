using AutoSensorMonitor.Service.Models;

namespace AutoSensorMonitor.Service.Services;

public interface ISensorReadingService {
    Task<Dictionary<string, double>> GetCurrentReadingsAsync();
    Task<SystemConfig> GetConfigurationAsync();
    Task UpdateConfigurationAsync(SystemConfig config);
}
