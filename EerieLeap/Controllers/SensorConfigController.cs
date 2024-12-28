using EerieLeap.Configuration;
using EerieLeap.Domain.SensorDomain.Services;
using EerieLeap.Domain.SensorDomain.Utilities;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace EerieLeap.Controllers;

[Route("api/v1/config/sensors")]
public partial class SensorConfigController : ConfigControllerBase {
    private readonly ISensorConfigurationService _sensorConfigService;

    public SensorConfigController(ILogger logger, ISensorConfigurationService sensorConfigService) : base(logger) =>
        _sensorConfigService = sensorConfigService;

    [HttpGet]
    public ActionResult<IEnumerable<SensorConfig>> GetConfigs() {
        try {
            var configs = _sensorConfigService.GetConfigurations();
            return Ok(configs);
        } catch (Exception ex) {
            LogGetConfigsError(ex.Message);
            return StatusCode(500, "Failed to retrieve sensor configurations");
        }
    }

    [HttpGet("{id}")]
    public ActionResult<SensorConfig> GetConfig([Required] string id) {
        try {
            if (!SensorIdValidator.IsValid(id))
                return BadRequest($"Invalid sensor Id format: '{id}'");

            var config = _sensorConfigService.GetConfiguration(id);

            if (config == null)
                return NotFound($"Sensor configuration with Id '{id}' not found");

            return Ok(config);
        } catch (Exception ex) {
            LogGetConfigError(id, ex.Message);
            return StatusCode(500, $"Failed to retrieve sensor configuration: {id}");
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateConfigsAsync(IEnumerable<SensorConfig> configs) {
        try {
            var result = await _sensorConfigService.UpdateConfigurationAsync(configs).ConfigureAwait(false);
            if (!result.Success) {
                return BadRequest(new {
                    Message = "Failed to update sensor configurations",
                    Errors = result.Errors.ToArray()
                });
            }

            return Ok();
        } catch (Exception ex) {
            LogUpdateConfigsError(ex.Message);
            return StatusCode(500, "An unexpected error occurred while updating sensor configurations");
        }
    }

    #region Logging

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to retrieve sensor configurations: {message}")]
    private partial void LogGetConfigsError(string message);

    [LoggerMessage(Level = LogLevel.Error, Message = "Sensor configuration not found: {sensorId}")]
    private partial void LogSensorNotFound(string sensorId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to retrieve sensor configuration {sensorId}: {message}")]
    private partial void LogGetConfigError(string sensorId, string message);

    [LoggerMessage(Level = LogLevel.Error, Message = "Validation error: {message}")]
    private partial void LogValidationError(string message);

    [LoggerMessage(Level = LogLevel.Error, Message = "Configuration error: {message}")]
    private partial void LogConfigurationError(string message);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to update sensor configurations: {message}")]
    private partial void LogUpdateConfigsError(string message);

    #endregion
}
