using AutoSensorMonitor.Services;
using AutoSensorMonitor.Aspects;
using AutoSensorMonitor.Configuration;
using AutoSensorMonitor.Types;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace AutoSensorMonitor.Controllers;

[ApiController]
[Route("api/v1")]
public class ConfigController : ControllerBase
{
    private readonly ILogger<ConfigController> _logger;
    private readonly ISensorReadingService _sensorService;

    public ConfigController(
        ILogger<ConfigController> logger,
        ISensorReadingService sensorService)
    {
        _logger = logger;
        _sensorService = sensorService;
    }

    [HttpGet("config")]
    public async Task<ActionResult<CombinedConfig>> GetConfig()
    {
        try
        {
            var adcConfig = await _sensorService.GetAdcConfigurationAsync();
            var sensorConfigs = await _sensorService.GetSensorConfigurationsAsync();
            
            var combinedConfig = new CombinedConfig
            {
                AdcConfig = adcConfig,
                SensorConfigs = sensorConfigs.ToList()
            };
            
            return Ok(combinedConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get combined configuration");
            return StatusCode(500, "Failed to get combined configuration");
        }
    }

    [HttpGet("config/adc")]
    public async Task<ActionResult<AdcConfig>> GetAdcConfig()
    {
        try
        {
            var config = await _sensorService.GetAdcConfigurationAsync();
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get ADC configuration");
            return StatusCode(500, "Failed to get ADC configuration");
        }
    }

    [HttpGet("config/sensors")]
    public async Task<ActionResult<List<SensorConfig>>> GetSensorConfigs()
    {
        try
        {
            var configs = await _sensorService.GetSensorConfigurationsAsync();
            return Ok(configs.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get sensor configurations");
            return StatusCode(500, "Failed to get sensor configurations");
        }
    }

    [HttpPost("config/adc")]
    [Validate]
    public async Task<IActionResult> UpdateAdcConfig([Required][FromBody] AdcConfig config)
    {
        try
        {
            await _sensorService.UpdateAdcConfigurationAsync(config);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update ADC configuration");
            return StatusCode(500, "Failed to update ADC configuration");
        }
    }

    [HttpPost("config/sensors")]
    [Validate]
    public async Task<IActionResult> UpdateSensorConfigs([Required][FromBody] List<SensorConfig> configs)
    {
        try
        {
            await _sensorService.UpdateSensorConfigurationsAsync(configs);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update sensor configurations");
            return StatusCode(500, "Failed to update sensor configurations");
        }
    }

    [HttpGet("sensors/{id}")]
    [ProducesResponseType(typeof(SensorConfig), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SensorConfig>> GetSensorConfig([FromRoute] string id)
    {
        try
        {
            var configs = await _sensorService.GetSensorConfigurationsAsync();
            var config = configs.FirstOrDefault(c => c.Id == id);
            
            if (config == null)
            {
                return NotFound($"Sensor configuration with Id '{id}' not found");
            }

            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get sensor configuration for Id {Id}", id);
            return StatusCode(500, "Failed to get sensor configuration");
        }
    }

    [HttpGet("readings")]
    public async Task<ActionResult<IEnumerable<ReadingResult>>> GetReadings()
    {
        try
        {
            var readings = await _sensorService.GetReadingsAsync();
            return Ok(readings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get sensor readings");
            return StatusCode(500, "Failed to get sensor readings");
        }
    }

    [HttpGet("readings/{id}")]
    public async Task<ActionResult<ReadingResult>> GetReading(string id)
    {
        try
        {
            var reading = await _sensorService.GetReadingAsync(id);
            if (reading == null)
            {
                return NotFound($"Reading for sensor Id '{id}' not found");
            }
            return Ok(reading);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get sensor reading for {Id}", id);
            return StatusCode(500, $"Failed to get sensor reading for {id}");
        }
    }
}
