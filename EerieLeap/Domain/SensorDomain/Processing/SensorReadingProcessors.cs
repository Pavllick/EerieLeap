// using System.Collections.Concurrent;
// using System.ComponentModel.DataAnnotations;
// using EerieLeap.Domain.SensorDomain.Models;

// namespace EerieLeap.Domain.SensorDomain.Processing;

// // Base processor that just stores readings
// public class BaseSensorReadingProcessor : ISensorReadingProcessor {
//     private readonly ConcurrentDictionary<string, List<SensorReading>> _readings = new();
//     private readonly ILogger _logger;

//     public BaseSensorReadingProcessor(ILogger logger) =>
//         _logger = logger;

//     public virtual async Task ProcessReadingAsync([Required] SensorReading reading) {
//         _readings.AddOrUpdate(
//             reading.Sensor.Id.Value,
//             new List<SensorReading> { reading },
//             (_, list) => {
//                 list.Add(reading);
//                 return list;
//             });

//         await Task.CompletedTask.ConfigureAwait(false);
//     }

//     public IEnumerable<SensorReading> GetLatestReadings() =>
//         _readings.Values.Select(list => list.LastOrDefault()).Where(r => r != null)!;
// }

// // Validates readings before processing
// public class ValidationProcessor : SensorReadingProcessorDecorator {
//     private readonly ISensorValidationService _validationService;

//     public ValidationProcessor(ISensorReadingProcessor processor, ISensorValidationService validationService) : base(processor) {
//         _validationService = validationService;
//     }

//     public override async Task ProcessReadingAsync(SensorReading reading) {
//         var result = _validationService.ValidateReading(reading);

//         if (result.IsValid) {
//             reading.Validate();
//             await Processor.ProcessReadingAsync(reading).ConfigureAwait(false);
//         }
//     }
// }

// // Logs readings
// public class LoggingProcessor : SensorReadingProcessorDecorator {
//     private readonly ILogger _logger;

//     public LoggingProcessor(ISensorReadingProcessor processor, ILogger logger) : base(processor) =>
//         _logger = logger;

//     public override async Task ProcessReadingAsync(SensorReading reading) {
//         _logger.LogInformation(
//             "Processing reading for sensor {SensorId}: {Value} at {Timestamp}",
//             reading.SensorId.Value,
//             reading.Value,
//             reading.Timestamp);

//         await Processor.ProcessReadingAsync(reading).ConfigureAwait(false);
//     }
// }

// // Publishes events for readings
// public class EventPublishingProcessor : SensorReadingProcessorDecorator {
//     private readonly IEventBus _eventBus;

//     public EventPublishingProcessor(ISensorReadingProcessor processor, IEventBus eventBus) : base(processor) =>
//         _eventBus = eventBus;

//     public override async Task ProcessReadingAsync(SensorReading reading) {
//         await Processor.ProcessReadingAsync(reading).ConfigureAwait(false);

//         var @event = new SensorReadingAddedEvent(reading);
//         await _eventBus.PublishAsync(@event);
//     }
// }
