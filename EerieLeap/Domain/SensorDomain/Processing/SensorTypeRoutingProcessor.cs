using System.ComponentModel.DataAnnotations;
using EerieLeap.Domain.SensorDomain.Models;
using EerieLeap.Domain.SensorDomain.Processing.SensorTypeProcessors;

namespace EerieLeap.Domain.SensorDomain.Processing;

public class SensorTypeRoutingProcessor : ISensorReadingProcessor {
    private readonly PhysicalSensorProcessor _physicalProcessor;
    private readonly VirtualSensorProcessor _virtualProcessor;

    public SensorTypeRoutingProcessor([Required] PhysicalSensorProcessor physicalProcessor, [Required] VirtualSensorProcessor virtualProcessor) {
        _physicalProcessor = physicalProcessor;
        _virtualProcessor = virtualProcessor;
    }

    public async Task ProcessReadingAsync([Required] SensorReading reading) {
        switch (reading.Sensor.Configuration.Type) {
            case SensorType.Physical:
                await _physicalProcessor.ProcessReadingAsync(reading).ConfigureAwait(false);
                break;
            case SensorType.Virtual:
                await _virtualProcessor.ProcessReadingAsync(reading).ConfigureAwait(false);
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(reading),
                    $"Unsupported sensor type: {reading.Sensor.Configuration.Type}");
        }
    }
}
