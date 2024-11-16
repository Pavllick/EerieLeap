using AutoSensorMonitor.Services;
using AutoSensorMonitor.Aspects;
using AutoSensorMonitor.Configuration;
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
                SensorConfigs = sensorConfigs
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
            return Ok(configs);
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

    [HttpGet("readings")]
    public async Task<ActionResult<Dictionary<string, double>>> GetReadings()
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
}
