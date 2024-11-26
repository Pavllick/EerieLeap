using EerieLeap.Services;
using EerieLeap.Configuration;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace EerieLeap.Controllers;

[ApiController]
[Route("api/v1/config")]
public partial class ConfigController : ConfigControllerBase {
    private readonly IAdcConfigurationService _adcService;
    private readonly ISensorReadingService _sensorService;

    public ConfigController(ILogger logger, IAdcConfigurationService adcService, ISensorReadingService sensorService)
        : base(logger) {
        _adcService = adcService;
        _sensorService = sensorService;
    }

    [HttpGet]
    public async Task<ActionResult<CombinedConfig>> GetConfig() {
        try {
            var adcConfig = await _adcService.GetConfigurationAsync().ConfigureAwait(false);
            var sensorConfigs = await _sensorService.GetSensorConfigurationsAsync().ConfigureAwait(false);

            var combinedConfig = new CombinedConfig {
                AdcConfig = adcConfig,
                SensorConfigs = sensorConfigs
            };

            return Ok(combinedConfig);
        } catch (JsonException ex) {
            LogConfigError(ex);
            return StatusCode(500, "Invalid configuration format");
        } catch (IOException ex) {
            LogConfigError(ex);
            return StatusCode(500, "Failed to read configuration files");
        } catch (InvalidOperationException ex) {
            LogConfigError(ex);
            return StatusCode(500, "Configuration is in an invalid state");
        }
    }

    #region Loggers

    [LoggerMessage(Level = LogLevel.Error, EventId = 1, Message = "Failed to get combined configuration")]
    private partial void LogConfigError(Exception ex);

    #endregion
}
