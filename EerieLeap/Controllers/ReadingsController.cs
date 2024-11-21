using EerieLeap.Services;
using EerieLeap.Types;
using EerieLeap.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace EerieLeap.Controllers;

[ApiController]
[Route("api/v1/readings")]
public class ReadingsController : ControllerBase {
    private readonly ILogger<ReadingsController> _logger;
    private readonly ISensorReadingService _sensorService;

    public ReadingsController(ISensorReadingService sensorService, ILogger<ReadingsController> logger) {
        _sensorService = sensorService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReadingResult>>> GetReadings() {
        try {
            var readings = await _sensorService.GetReadingsAsync().ConfigureAwait(false);
            return Ok(readings);
        } catch (Exception ex) {
            _logger.LogError(ex, "Failed to get sensor readings");
            return StatusCode(500, "Failed to get sensor readings");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ReadingResult>> GetReading(string id) {
        try {
            if (!SensorIdValidator.IsValid(id))
                return BadRequest($"Invalid sensor Id format: '{id}'");

            var reading = await _sensorService.GetReadingAsync(id).ConfigureAwait(false);
            if (reading == null)
                return NotFound($"Reading for sensor Id '{id}' not found");

            return Ok(reading);
        } catch (Exception ex) {
            _logger.LogError(ex, "Failed to get sensor reading for {Id}", id);
            return StatusCode(500, $"Failed to get sensor reading for {id}");
        }
    }
}
