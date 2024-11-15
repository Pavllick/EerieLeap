using System.ComponentModel.DataAnnotations;
using AutoSensorMonitor.Service.Hardware;

namespace AutoSensorMonitor.Service.Models;

public sealed class SystemConfig
{
    [Required]
    public AdcConfig AdcConfig { get; init; } = new();
    
    [Required]
    [MinLength(1)]
    public List<SensorConfig> Sensors { get; init; } = new();
}
