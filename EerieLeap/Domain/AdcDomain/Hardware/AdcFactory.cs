namespace EerieLeap.Domain.AdcDomain.Hardware;

public sealed partial class AdcFactory {
    private readonly ILogger _logger;
    private readonly bool _isDevelopment;

    public AdcFactory(ILogger logger, bool isDevelopment = true) {
        _logger = logger;
        _isDevelopment = isDevelopment;
    }

    public IAdc CreateAdc() {
        if (_isDevelopment) {
            LogUsingMockAdc();

            return new MockAdc();
        }

        LogCreatingAdc();

        return new SpiAdc(_logger);
    }

    #region Loggers

    [LoggerMessage(Level = LogLevel.Information, Message = "Creating ADC")]
    private partial void LogCreatingAdc();

    [LoggerMessage(Level = LogLevel.Information, Message = "Using mock ADC for testing")]
    private partial void LogUsingMockAdc();

    #endregion
}
