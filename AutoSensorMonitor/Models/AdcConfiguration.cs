using System.ComponentModel.DataAnnotations;

namespace AutoSensorMonitor.Service.Models;

public class AdcConfiguration
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
}
