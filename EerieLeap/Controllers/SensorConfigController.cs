using EerieLeap.Configuration;
using EerieLeap.Services;
using EerieLeap.Utilities;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace EerieLeap.Controllers;

[Route("api/v1/config/sensors")]
public partial class SensorConfigController : ConfigControllerBase {
    private readonly ISensorReadingService _sensorService;

    public SensorConfigController(ILogger logger, ISensorReadingService sensorService) : base(logger) =>
        _sensorService = sensorService;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SensorConfig>>> GetConfigs() {
        try {
            var configs = await _sensorService.GetSensorConfigurationsAsync().ConfigureAwait(false);
            return Ok(configs);
        } catch (JsonException ex) {
            LogConfigsError(ex);
            return StatusCode(500, "Invalid sensor configuration format");
        } catch (IOException ex) {
            LogConfigsError(ex);
            return StatusCode(500, "Failed to read sensor configuration file");
        } catch (InvalidOperationException ex) {
            LogConfigsError(ex);
            return StatusCode(500, "Sensor configuration is in an invalid state");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SensorConfig>> GetConfig([FromRoute] string id) {
        try {
            if (!SensorIdValidator.IsValid(id))
                return BadRequest($"Invalid sensor Id format: '{id}'");

            var configs = await _sensorService.GetSensorConfigurationsAsync().ConfigureAwait(false);
            var config = configs.FirstOrDefault(c => c.Id == id);

            if (config == null)
                return NotFound($"Sensor configuration with Id '{id}' not found");

            return Ok(config);
        } catch (JsonException ex) {
            LogConfigError(id, ex);
            return StatusCode(500, "Invalid sensor configuration format");
        } catch (IOException ex) {
            LogConfigError(id, ex);
            return StatusCode(500, "Failed to read sensor configuration file");
        } catch (InvalidOperationException ex) {
            LogConfigError(id, ex);
            return StatusCode(500, "Sensor configuration is in an invalid state");
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateConfigs([FromBody] IEnumerable<SensorConfig> configs) {
        try {
            var configsList = configs.ToList();

            var seenIds = new HashSet<string>();
            foreach (var config in configsList) {
                if (!SensorIdValidator.IsValid(config.Id))
                    return BadRequest($"Invalid sensor Id format: '{config.Id}'");

                if (!seenIds.Add(config.Id))
                    return BadRequest($"Duplicate sensor ID found: '{config.Id}'");
            }

            await _sensorService.UpdateConfigurationAsync(configsList).ConfigureAwait(false);

            return Ok();
        } catch (JsonException ex) {
            LogConfigsUpdateError(ex);
            return StatusCode(500, "Failed to serialize sensor configurations");
        } catch (IOException ex) {
            LogConfigsUpdateError(ex);
            return StatusCode(500, "Failed to write sensor configuration file");
        } catch (ValidationException ex) {
            LogConfigsUpdateError(ex);
            return BadRequest(ex.Message);
        }
    }

    #region Loggers

    [LoggerMessage(Level = LogLevel.Error, EventId = 1, Message = "Failed to get sensor configurations")]
    private partial void LogConfigsError(Exception ex);

    [LoggerMessage(Level = LogLevel.Error, EventId = 2, Message = "Failed to get sensor configuration for Id {id}")]
    private partial void LogConfigError(string id, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, EventId = 3, Message = "Failed to update sensor configurations")]
    private partial void LogConfigsUpdateError(Exception ex);

    #endregion
}
