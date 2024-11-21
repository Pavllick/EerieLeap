using EerieLeap.Services;
using EerieLeap.Aspects;
using EerieLeap.Configuration;
using EerieLeap.Utilities;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace EerieLeap.Controllers;

[ApiController]
[Route("api/v1/config")]
public partial class ConfigController : ControllerBase {
    private readonly ILogger _logger;
    private readonly ISensorReadingService _sensorService;

    public ConfigController(ILogger logger, ISensorReadingService sensorService) {
        _logger = logger;
        _sensorService = sensorService;
    }

    [HttpGet]
    public async Task<ActionResult<CombinedConfig>> GetConfig() {
        try {
            var adcConfig = await _sensorService.GetAdcConfigurationAsync().ConfigureAwait(false);
            var sensorConfigs = await _sensorService.GetSensorConfigurationsAsync().ConfigureAwait(false);

            var combinedConfig = new CombinedConfig {
                AdcConfig = adcConfig,
                SensorConfigs = sensorConfigs
            };

            return Ok(combinedConfig);
        } catch (Exception ex) {
            LogCombinedConfigError(ex);
            return StatusCode(500, "Failed to get combined configuration");
        }
    }

    [HttpGet("adc")]
    public async Task<ActionResult<AdcConfig>> GetAdcConfig() {
        try {
            var config = await _sensorService.GetAdcConfigurationAsync().ConfigureAwait(false);
            if (config == null)
                return NotFound("ADC configuration not found");

            return Ok(config);
        } catch (Exception ex) {
            LogAdcConfigError(ex);
            return StatusCode(500, "Failed to get ADC configuration");
        }
    }

    [HttpPost("adc")]
    [Validate]
    public async Task<IActionResult> UpdateAdcConfig([Required][FromBody] AdcConfig config) {
        try {
            await _sensorService.UpdateAdcConfigurationAsync(config).ConfigureAwait(false);
            return Ok();
        } catch (Exception ex) {
            LogAdcConfigUpdateError(ex);
            return StatusCode(500, "Failed to update ADC configuration");
        }
    }

    [HttpGet("sensors")]
    public async Task<ActionResult<IEnumerable<SensorConfig>>> GetSensorConfigs() {
        try {
            var configs = await _sensorService.GetSensorConfigurationsAsync().ConfigureAwait(false);
            return Ok(configs);
        } catch (Exception ex) {
            LogSensorConfigsError(ex);
            return StatusCode(500, "Failed to get sensor configurations");
        }
    }

    [HttpGet("sensors/{id}")]
    public async Task<ActionResult<SensorConfig>> GetSensorConfig([FromRoute] string id) {
        try {
            if (!SensorIdValidator.IsValid(id))
                return BadRequest($"Invalid sensor Id format: '{id}'");

            var configs = await _sensorService.GetSensorConfigurationsAsync().ConfigureAwait(false);
            var config = configs.FirstOrDefault(c => c.Id == id);

            if (config == null)
                return NotFound($"Sensor configuration with Id '{id}' not found");

            return Ok(config);
        } catch (Exception ex) {
            LogSensorConfigError(id, ex);
            return StatusCode(500, "Failed to get sensor configuration");
        }
    }

    [HttpPost("sensors")]
    [Validate]
    public async Task<IActionResult> UpdateSensorConfigs([Required][FromBody] IEnumerable<SensorConfig> configs) {
        try {
            // Validate sensor IDs
            foreach (var config in configs)
                if (!SensorIdValidator.IsValid(config.Id))
                    return BadRequest($"Invalid sensor Id format: '{config.Id}'");

            // Check for duplicate IDs
            var duplicateIds = configs
                .GroupBy(c => c.Id)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateIds.Any())
                return BadRequest($"Duplicate sensor IDs found: {string.Join(", ", duplicateIds)}");

            await _sensorService.UpdateSensorConfigurationsAsync(configs).ConfigureAwait(false);
            return Ok();
        } catch (Exception ex) {
            LogSensorConfigsUpdateError(ex);
            return StatusCode(500, "Failed to update sensor configurations");
        }
    }

    #region Loggers

    [LoggerMessage(Level = LogLevel.Error, EventId = 1, Message = "Failed to get combined configuration")]
    private partial void LogCombinedConfigError(Exception ex);

    [LoggerMessage(Level = LogLevel.Error, EventId = 2, Message = "Failed to get ADC configuration")]
    private partial void LogAdcConfigError(Exception ex);

    [LoggerMessage(Level = LogLevel.Error, EventId = 3, Message = "Failed to update ADC configuration")]
    private partial void LogAdcConfigUpdateError(Exception ex);

    [LoggerMessage(Level = LogLevel.Error, EventId = 4, Message = "Failed to get sensor configurations")]
    private partial void LogSensorConfigsError(Exception ex);

    [LoggerMessage(Level = LogLevel.Error, EventId = 5, Message = "Failed to get sensor configuration for Id {id}")]
    private partial void LogSensorConfigError(string id, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, EventId = 6, Message = "Failed to update sensor configurations")]
    private partial void LogSensorConfigsUpdateError(Exception ex);

    #endregion
}
