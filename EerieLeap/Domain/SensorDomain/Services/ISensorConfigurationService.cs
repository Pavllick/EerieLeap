using EerieLeap.Configuration;
using EerieLeap.Types;

namespace EerieLeap.Domain.SensorDomain.Services;

public interface ISensorConfigurationService : IDisposable {
    Task InitializeAsync(CancellationToken stoppingToken);
    IReadOnlyList<SensorConfig> GetConfigurations();
    SensorConfig? GetConfiguration(string sensorId);
    Task<ConfigurationResult> UpdateConfigurationAsync(IEnumerable<SensorConfig> configs);
}
