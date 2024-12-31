using EerieLeap.Domain.SensorDomain.Models;

namespace EerieLeap.Domain.SensorDomain.Processing;

internal interface ISensorReadingProcessor {
    Task ProcessReadingAsync(SensorReading reading);
}
