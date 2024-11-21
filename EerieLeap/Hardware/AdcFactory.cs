namespace EerieLeap.Hardware;

public sealed partial class AdcFactory : IAdcFactory {
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

        return new Adc(_logger);
    }

    #region Loggers

    [LoggerMessage(Level = LogLevel.Information, EventId = 1, Message = "Creating ADC")]
    private partial void LogCreatingAdc();

    [LoggerMessage(Level = LogLevel.Information, EventId = 2, Message = "Using mock ADC for testing")]
    private partial void LogUsingMockAdc();

    #endregion
}
