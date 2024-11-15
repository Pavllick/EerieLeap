using Microsoft.Extensions.Logging;

namespace AutoSensorMonitor.Service.Hardware;

public sealed class AdcFactory {
    private readonly ILogger<AdcFactory> _logger;
    private readonly bool _isDevelopment;

    public AdcFactory(ILogger<AdcFactory> logger, bool isDevelopment = true) {
        _logger = logger;
        _isDevelopment = isDevelopment;
    }

    public IAdc CreateAdc(string type) {
        ArgumentException.ThrowIfNullOrEmpty(type);

        if (_isDevelopment) {
            _logger.LogInformation("Using MockAdc for development environment");
            return new MockAdc();
        }

        return type.ToLowerInvariant() switch {
            "ads7953" => new Ads7953(),
            _ => throw new ArgumentException($"Unsupported ADC type: {type}")
        };
    }
}
