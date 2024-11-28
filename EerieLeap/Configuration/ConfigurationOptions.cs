using System.ComponentModel.DataAnnotations;

namespace EerieLeap.Configuration;

public class ConfigurationOptions
{
    [Required]
    public string ConfigurationPath { get; set; } = string.Empty;
}
