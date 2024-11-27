using EerieLeap.Configuration;

namespace EerieLeap.Services;

public interface ISensorConfigurationService : IDisposable {
    Task InitializeAsync();
    IReadOnlyList<SensorConfig> GetConfigurations();
    Task<bool> UpdateConfigurationAsync(IEnumerable<SensorConfig> configs);
    SensorConfig? GetConfiguration(string sensorId);
    bool TryGetConfiguration(string sensorId, out SensorConfig? config);
}
