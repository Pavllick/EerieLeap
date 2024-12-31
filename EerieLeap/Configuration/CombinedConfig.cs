using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using ValidationProcessor.DataAnnotations;

namespace EerieLeap.Configuration;

[IgnoreCustomValidation]
public class CombinedConfig {
    [Required]
    public Settings Settings { get; set; } = new();

    [Required]
    public AdcConfig AdcConfig { get; set; } = new();

    [Required]
    public IEnumerable<SensorConfig> SensorConfigs { get; set; } = new Collection<SensorConfig>();
}
