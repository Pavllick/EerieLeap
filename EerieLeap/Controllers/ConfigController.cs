using EerieLeap.Services;
using EerieLeap.Aspects;
using EerieLeap.Configuration;
using EerieLeap.Utilities;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace EerieLeap.Controllers;

[ApiController]
[Route("api/v1/config")]
public class ConfigController : ControllerBase
{
    private readonly ILogger<ConfigController> _logger;
    private readonly ISensorReadingService _sensorService;

    public ConfigController(ILogger<ConfigController> logger, ISensorReadingService sensorService)
    {
        _logger = logger;
        _sensorService = sensorService;
    }

    [HttpGet]
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

    [HttpGet("adc")]
    public async Task<ActionResult<AdcConfig>> GetAdcConfig()
    {
        try
        {
            var config = await _sensorService.GetAdcConfigurationAsync();
            if (config == null)
                return NotFound("ADC configuration not found");

            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get ADC configuration");
            return StatusCode(500, "Failed to get ADC configuration");
        }
    }

    [HttpPost("adc")]
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

    [HttpGet("sensors")]
    public async Task<ActionResult<IEnumerable<SensorConfig>>> GetSensorConfigs()
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

    [HttpGet("sensors/{id}")]
    public async Task<ActionResult<SensorConfig>> GetSensorConfig([FromRoute] string id)
    {
        try
        {
            if (!SensorIdValidator.IsValid(id))
                return BadRequest($"Invalid sensor Id format: '{id}'");

            var configs = await _sensorService.GetSensorConfigurationsAsync();
            var config = configs.FirstOrDefault(c => c.Id == id);
            
            if (config == null)
                return NotFound($"Sensor configuration with Id '{id}' not found");

            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get sensor configuration for Id {Id}", id);
            return StatusCode(500, "Failed to get sensor configuration");
        }
    }

    [HttpPost("sensors")]
    [Validate]
    public async Task<IActionResult> UpdateSensorConfigs([Required][FromBody] List<SensorConfig> configs)
    {
        try
        {
            // Validate sensor IDs
            foreach (var config in configs)
                if (!SensorIdValidator.IsValid(config.Id))
                    return BadRequest($"Invalid sensor Id format: '{config.Id}'");

            // Check for duplicate IDs
            var duplicateIds = configs.GroupBy(c => c.Id)
                                    .Where(g => g.Count() > 1)
                                    .Select(g => g.Key)
                                    .ToList();

            if (duplicateIds.Any())
                return BadRequest($"Duplicate sensor IDs found: {string.Join(", ", duplicateIds)}");

            await _sensorService.UpdateSensorConfigurationsAsync(configs);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update sensor configurations");
            return StatusCode(500, "Failed to update sensor configurations");
        }
    }
}
