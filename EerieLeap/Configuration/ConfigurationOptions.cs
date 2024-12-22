using System.ComponentModel.DataAnnotations;
using EerieLeap.Utilities.DataAnnotations;

namespace EerieLeap.Configuration;

[IgnoreCustomValidation]
public class ConfigurationOptions
{
    [Required]
    public string ConfigurationPath { get; set; } = string.Empty;
}
