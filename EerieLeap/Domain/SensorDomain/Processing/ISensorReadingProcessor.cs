using EerieLeap.Domain.SensorDomain.Models;

namespace EerieLeap.Domain.SensorDomain.Processing;

public interface ISensorReadingProcessor {
    Task ProcessReadingAsync(SensorReading reading);
}
