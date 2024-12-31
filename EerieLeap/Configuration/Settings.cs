using ValidationProcessor.DataAnnotations;

namespace EerieLeap.Configuration;

[IgnoreCustomValidation]
public class Settings {
    public int ConfigurationLoadRetryMs { get; set; } = 5000;

    public int ProcessSensorsIntervalMs { get; set; } = 1000;
}
