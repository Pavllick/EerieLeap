using EerieLeap.Configuration;

namespace EerieLeap.Services;

public interface ISensorConfigurationService : IDisposable {
    Task InitializeAsync();
    IReadOnlyList<SensorConfig> GetConfigurations();
    SensorConfig? GetConfiguration(string sensorId);
    Task<bool> UpdateConfigurationAsync(IEnumerable<SensorConfig> configs);
    bool TryGetConfiguration(string sensorId, out SensorConfig? config);
}
