using System.ComponentModel.DataAnnotations;

namespace EerieLeap.Configuration;

public class CombinedConfig
{
    [Required]
    public AdcConfig AdcConfig { get; set; } = new();

    [Required]
    public List<SensorConfig> SensorConfigs { get; set; } = new();
}
