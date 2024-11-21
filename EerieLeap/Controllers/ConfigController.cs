using EerieLeap.Services;
using EerieLeap.Aspects;
using EerieLeap.Configuration;
using EerieLeap.Utilities;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

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
        } catch (JsonException ex) {
            LogCombinedConfigError(ex);
            return StatusCode(500, "Invalid configuration format");
        } catch (IOException ex) {
            LogCombinedConfigError(ex);
            return StatusCode(500, "Failed to read configuration files");
        } catch (InvalidOperationException ex) {
            LogCombinedConfigError(ex);
            return StatusCode(500, "Configuration is in an invalid state");
        }
    }

    [HttpGet("adc")]
    public async Task<ActionResult<AdcConfig>> GetAdcConfig() {
        try {
            var config = await _sensorService.GetAdcConfigurationAsync().ConfigureAwait(false);
            if (config == null)
                return NotFound("ADC configuration not found");

            return Ok(config);
        } catch (JsonException ex) {
            LogAdcConfigError(ex);
            return StatusCode(500, "Invalid ADC configuration format");
        } catch (IOException ex) {
            LogAdcConfigError(ex);
            return StatusCode(500, "Failed to read ADC configuration file");
        } catch (InvalidOperationException ex) {
            LogAdcConfigError(ex);
            return StatusCode(500, "ADC configuration is in an invalid state");
        }
    }

    [HttpPost("adc")]
    [Validate]
    public async Task<IActionResult> UpdateAdcConfig([Required][FromBody] AdcConfig config) {
        try {
            await _sensorService.UpdateAdcConfigurationAsync(config).ConfigureAwait(false);
            return Ok();
        } catch (JsonException ex) {
            LogAdcConfigUpdateError(ex);
            return StatusCode(500, "Failed to serialize ADC configuration");
        } catch (IOException ex) {
            LogAdcConfigUpdateError(ex);
            return StatusCode(500, "Failed to write ADC configuration file");
        } catch (ValidationException ex) {
            LogAdcConfigUpdateError(ex);
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("sensors")]
    public async Task<ActionResult<IEnumerable<SensorConfig>>> GetSensorConfigs() {
        try {
            var configs = await _sensorService.GetSensorConfigurationsAsync().ConfigureAwait(false);
            return Ok(configs);
        } catch (JsonException ex) {
            LogSensorConfigsError(ex);
            return StatusCode(500, "Invalid sensor configuration format");
        } catch (IOException ex) {
            LogSensorConfigsError(ex);
            return StatusCode(500, "Failed to read sensor configuration file");
        } catch (InvalidOperationException ex) {
            LogSensorConfigsError(ex);
            return StatusCode(500, "Sensor configuration is in an invalid state");
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
        } catch (JsonException ex) {
            LogSensorConfigError(id, ex);
            return StatusCode(500, "Invalid sensor configuration format");
        } catch (IOException ex) {
            LogSensorConfigError(id, ex);
            return StatusCode(500, "Failed to read sensor configuration file");
        } catch (InvalidOperationException ex) {
            LogSensorConfigError(id, ex);
            return StatusCode(500, "Sensor configuration is in an invalid state");
        }
    }

    [HttpPost("sensors")]
    [Validate]
    public async Task<IActionResult> UpdateSensorConfigs([FromBody] IEnumerable<SensorConfig> configs) {
        ArgumentNullException.ThrowIfNull(configs);

        try {
            var configsList = configs.ToList();

            var seenIds = new HashSet<string>();
            foreach (var config in configsList) {
                if (!SensorIdValidator.IsValid(config.Id))
                    return BadRequest($"Invalid sensor Id format: '{config.Id}'");

                if (!seenIds.Add(config.Id))
                    return BadRequest($"Duplicate sensor ID found: '{config.Id}'");
            }

            await _sensorService.UpdateSensorConfigurationsAsync(configsList).ConfigureAwait(false);

            return Ok();
        } catch (JsonException ex) {
            LogSensorConfigsUpdateError(ex);
            return StatusCode(500, "Failed to serialize sensor configurations");
        } catch (IOException ex) {
            LogSensorConfigsUpdateError(ex);
            return StatusCode(500, "Failed to write sensor configuration file");
        } catch (ValidationException ex) {
            LogSensorConfigsUpdateError(ex);
            return BadRequest(ex.Message);
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
