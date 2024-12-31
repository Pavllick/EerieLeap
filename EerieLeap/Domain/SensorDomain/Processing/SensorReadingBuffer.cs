using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using EerieLeap.Domain.SensorDomain.Models;

namespace EerieLeap.Domain.SensorDomain.Processing;

internal class SensorReadingBuffer {
    private readonly ConcurrentDictionary<string, SensorReading> _lastReadings = new();

    public void AddReading([Required] SensorReading reading) =>
        _lastReadings.AddOrUpdate(reading.Sensor.Id.Value, reading, (_, _) => reading);

    public IEnumerable<SensorReading> GetAllReadings() =>
        _lastReadings.Values;

    public SensorReading? GetReading([Required] string id) =>
        _lastReadings.TryGetValue(id, out var value)
            ? value
            : null;

    public void Clear() =>
        _lastReadings.Clear();
}
