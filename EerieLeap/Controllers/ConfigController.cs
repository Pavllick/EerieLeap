using EerieLeap.Services;
using EerieLeap.Configuration;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace EerieLeap.Controllers;

[ApiController]
[Route("api/v1/config")]
public partial class ConfigController : ConfigControllerBase {
    private readonly IAdcConfigurationService _adcService;
    private readonly ISensorConfigurationService _sensorConfigService;

    public ConfigController(ILogger logger, IAdcConfigurationService adcService, ISensorConfigurationService sensorConfigService)
        : base(logger) {
        _adcService = adcService;
        _sensorConfigService = sensorConfigService;
    }

    [HttpGet]
    public ActionResult<CombinedConfig> GetConfig() {
        try {
            var combinedConfig = new CombinedConfig {
                AdcConfig = _adcService.GetConfiguration(),
                SensorConfigs = _sensorConfigService.GetConfigurations()
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
