using EerieLeap.Domain.SensorDomain.Models;

namespace EerieLeap.Domain.SensorDomain.Services;

public interface ISensorReadingService {
    Task<IEnumerable<SensorReading>> GetReadingsAsync();
    Task<SensorReading?> GetReadingAsync(string id);
    Task WaitForInitializationAsync();
}
