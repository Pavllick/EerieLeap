using System.ComponentModel.DataAnnotations;
using ValidationProcessor.DataAnnotations;

namespace EerieLeap.Configuration;

[IgnoreCustomValidation]
public class ConfigurationOptions {
    [Required]
    public string ConfigurationPath { get; set; } = string.Empty;

    public int ConfigurationLoadRetryMs { get; set; } = 5000;

    public int ProcessSensorsIntervalMs { get; set; } = 1000;
}
