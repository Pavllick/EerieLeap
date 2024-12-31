using EerieLeap.Domain.AdcDomain.Services;
using EerieLeap.Domain.SensorDomain.Services;
using EerieLeap.Configuration;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace EerieLeap.Controllers;

[ApiController]
[Route("api/v1/config")]
public partial class ConfigController : ConfigControllerBase {
    private readonly Settings _settings;
    private readonly IAdcConfigurationService _adcService;
    private readonly ISensorConfigurationService _sensorConfigService;

    public ConfigController(
        ILogger logger,
        [Required] IOptions<Settings> settings,
        [Required] IAdcConfigurationService adcService,
        [Required] ISensorConfigurationService sensorConfigService)
    : base(logger) {

        _settings = settings.Value;
        _adcService = adcService;
        _sensorConfigService = sensorConfigService;
    }

    [HttpGet]
    public ActionResult<CombinedConfig> GetConfig() {
        try {
            var combinedConfig = new CombinedConfig {
                Settings = _settings,
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

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to get combined configuration")]
    private partial void LogConfigError(Exception ex);

    #endregion
}
