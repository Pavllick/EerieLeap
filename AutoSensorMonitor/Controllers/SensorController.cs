using AutoSensorMonitor.Service.Models;
using AutoSensorMonitor.Service.Services;
using AutoSensorMonitor.Service.Aspects;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoSensorMonitor.Service.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class SensorController : ControllerBase {
    private readonly ISensorReadingService _sensorService;
    private readonly ILogger<SensorController> _logger;

    public SensorController(
        ISensorReadingService sensorService, 
        ILogger<SensorController> logger)
    {
        _sensorService = sensorService;
        _logger = logger;
    }

    [HttpGet("readings")]
    [ProducesResponseType(typeof(Dictionary<string, double>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Dictionary<string, double>>> GetReadings()
    {
        try
        {
            var readings = await _sensorService.GetCurrentReadingsAsync().ConfigureAwait(false);
            return Ok(readings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sensor readings");
            return StatusCode(500, "Error retrieving sensor readings");
        }
    }

    [HttpGet("config")]
    [ProducesResponseType(typeof(SystemConfig), StatusCodes.Status200OK)]
    public async Task<ActionResult<SystemConfig>> GetConfig()
    {
        try
        {
            var config = await _sensorService.GetConfigurationAsync().ConfigureAwait(false);
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sensor configuration");
            return StatusCode(500, "Error retrieving sensor configuration");
        }
    }

    [HttpPut("config")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Validate]
    public async Task<IActionResult> UpdateConfig([Required][FromBody] SystemConfig config)
    {
        try
        {
            await _sensorService.UpdateConfigurationAsync(config).ConfigureAwait(false);
            return Ok();
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Invalid configuration provided");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating sensor configuration");
            return StatusCode(500, "Error updating sensor configuration");
        }
    }
}
