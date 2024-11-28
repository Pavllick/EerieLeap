using EerieLeap.Configuration;
using EerieLeap.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace EerieLeap.Controllers;

[Route("api/v1/config/adc")]
public partial class AdcConfigController : ConfigControllerBase {
    private readonly IAdcConfigurationService _adcService;

    public AdcConfigController(IAdcConfigurationService adcService, ILogger logger) : base(logger) =>
        _adcService = adcService;

    [HttpGet]
    public ActionResult<AdcConfig> GetConfiguration() {
        try {
            var config = _adcService.GetConfiguration();

            return config == null
                ? NotFound("ADC configuration not found")
                : Ok(config);
        } catch (JsonException ex) {
            LogGetConfigurationError(ex);
            return StatusCode(500, "Invalid ADC configuration format");
        } catch (IOException ex) {
            LogGetConfigurationError(ex);
            return StatusCode(500, "Failed to read ADC configuration file");
        } catch (InvalidOperationException ex) {
            LogGetConfigurationError(ex);
            return StatusCode(500, "ADC configuration is in an invalid state");
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateConfigurationAsync([Required] AdcConfig config) {
        try {
            await _adcService.UpdateConfigurationAsync(config).ConfigureAwait(false);

            return Ok();
        } catch (JsonException ex) {
            LogUpdateConfigurationError(ex);
            return StatusCode(500, "Failed to serialize ADC configuration");
        } catch (IOException ex) {
            LogUpdateConfigurationError(ex);
            return StatusCode(500, "Failed to write ADC configuration file");
        } catch (ValidationException ex) {
            LogValidationError(ex);
            return BadRequest(ex.Message);
        }
    }

    #region Loggers

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to get ADC configuration")]
    private partial void LogGetConfigurationError(Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to update ADC configuration")]
    private partial void LogUpdateConfigurationError(Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "ADC configuration validation failed")]
    private partial void LogValidationError(Exception ex);

    #endregion
}
