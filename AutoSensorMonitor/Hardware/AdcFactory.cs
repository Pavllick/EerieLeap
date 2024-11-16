using Microsoft.Extensions.Logging;

namespace AutoSensorMonitor.Hardware;

public sealed class AdcFactory
{
    private readonly ILogger<AdcFactory> _logger;
    private readonly ILogger<Adc> _adcLogger;
    private readonly bool _isDevelopment;

    public AdcFactory(
        ILogger<AdcFactory> logger,
        ILogger<Adc> adcLogger,
        bool isDevelopment = true)
    {
        _logger = logger;
        _adcLogger = adcLogger;
        _isDevelopment = isDevelopment;
    }

    public IAdc CreateAdc()
    {
        if (_isDevelopment)
        {
            _logger.LogInformation("Using MockAdc for development environment");
            return new MockAdc();
        }

        _logger.LogInformation("Creating ADC");
        return new Adc(_adcLogger);
    }
}
