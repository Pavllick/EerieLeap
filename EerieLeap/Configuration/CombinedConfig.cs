using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using EerieLeap.Utilities.DataAnnotations;

namespace EerieLeap.Configuration;

[IgnoreCustomValidation]
public class CombinedConfig {
    [Required]
    public AdcConfig AdcConfig { get; set; } = new();

    [Required]
    public IEnumerable<SensorConfig> SensorConfigs { get; set; } = new Collection<SensorConfig>();
}
