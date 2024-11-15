using System.ComponentModel.DataAnnotations;
using AutoSensorMonitor.Service.Models;

namespace AutoSensorMonitor.Service.Hardware;

public sealed class AdcConfig
{
    [Required]
    public string Type { get; set; } = string.Empty;
    
    [Required]
    [Range(0, int.MaxValue)]
    public int BusId { get; set; }
    
    [Required]
    [Range(0, int.MaxValue)]
    public int ChipSelect { get; set; }
    
    [Required]
    [Range(1_000, 100_000_000)]
    public int ClockFrequency { get; set; }
    
    public Dictionary<string, string> AdditionalSettings { get; set; } = new();

    public static AdcConfig FromConfiguration(AdcConfiguration config)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));
        
        return new AdcConfig
        {
            Type = config.Type,
            BusId = config.BusId,
            ChipSelect = config.ChipSelect,
            ClockFrequency = config.ClockFrequency,
            AdditionalSettings = config.AdditionalSettings
        };
    }
}
