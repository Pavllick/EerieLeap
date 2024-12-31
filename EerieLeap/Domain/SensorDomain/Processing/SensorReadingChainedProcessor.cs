using System.ComponentModel.DataAnnotations;
using EerieLeap.Domain.SensorDomain.Models;

namespace EerieLeap.Domain.SensorDomain.Processing;

internal class SensorReadingChainedProcessor : ISensorReadingProcessor {
    private readonly ISensorReadingProcessor[] _processors;

    public SensorReadingChainedProcessor(params ISensorReadingProcessor[] processors) =>
        _processors = processors;

    public async Task ProcessReadingAsync([Required] SensorReading reading) {
        foreach (var processor in _processors) {
            await processor.ProcessReadingAsync(reading).ConfigureAwait(false);

            // Stop processing if there was an error
            if (reading.Status == ReadingStatus.Error)
                break;
        }
    }
}
