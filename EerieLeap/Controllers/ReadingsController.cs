using EerieLeap.Domain.SensorDomain.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;
using EerieLeap.Domain.SensorDomain.Services;
using EerieLeap.Domain.SensorDomain.Utilities;

namespace EerieLeap.Controllers;

[ApiController]
[Route("api/v1/readings")]
public partial class ReadingsController : ControllerBase {
    private readonly ILogger _logger;
    private readonly ISensorReadingService _sensorService;

    public ReadingsController(ISensorReadingService sensorService, ILogger logger) {
        _sensorService = sensorService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SensorReading>>> GetReadingsAsync() {
        try {
            var readings = await _sensorService.GetReadingsAsync().ConfigureAwait(false);
            return Ok(readings);
        } catch (InvalidOperationException ex) {
            LogReadingsError(ex);
            return StatusCode(500, "Sensor readings are not available - service may be initializing");
        } catch (JsonException ex) {
            LogReadingsError(ex);
            return StatusCode(500, "Failed to process sensor readings data");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SensorReading>> GetReadingAsync([FromRoute][Required] string id) {
        try {
            if (!SensorIdValidator.IsValid(id))
                return BadRequest($"Invalid sensor Id format: '{id}'");

            var reading = await _sensorService.GetReadingAsync(id).ConfigureAwait(false);
            if (reading == null)
                return NotFound($"Reading for sensor Id '{id}' not found");

            return Ok(reading);
        } catch (InvalidOperationException ex) {
            LogReadingError(id, ex);
            return StatusCode(500, $"Sensor reading for {id} is not available - service may be initializing");
        } catch (ArgumentException ex) {
            LogReadingError(id, ex);
            return BadRequest($"Invalid sensor configuration for {id}");
        }
    }

    #region Loggers

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to get sensor readings")]
    private partial void LogReadingsError(Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to get sensor reading for {id}")]
    private partial void LogReadingError(string id, Exception ex);

    #endregion
}
