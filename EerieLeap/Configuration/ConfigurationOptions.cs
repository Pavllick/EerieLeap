using System.ComponentModel.DataAnnotations;
using Weavers.DataAnnotations;

namespace EerieLeap.Configuration;

[IgnoreCustomValidation]
public class ConfigurationOptions
{
    [Required]
    public string ConfigurationPath { get; set; } = string.Empty;
}
