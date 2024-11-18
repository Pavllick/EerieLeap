using System.ComponentModel.DataAnnotations;
using EerieLeap.Types;
using EerieLeap.Utilities.DataAnnotations;

namespace EerieLeap.Configuration;

public class SensorConfig
{
    private string _id = Guid.NewGuid().ToString("N");
    private string _name = string.Empty;

    [Required]
    [RegularExpression("^[a-zA-Z0-9_]+$", ErrorMessage = "Sensor ID can only contain letters, numbers, and underscores")]
    public string Id 
    { 
        get => _id;
        set => _id = value.ToLower().Replace(" ", "_");
    }

    [Required]
    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            if (string.IsNullOrEmpty(_id) || _id == Guid.NewGuid().ToString("N"))
            {
                Id = value;
            }
        }
    }

    [Required]
    public SensorType Type { get; set; }

    [Required]
    public string Unit { get; set; } = string.Empty;

    [RequiredForPhysicalSensor]
    [Range(0, 31, ErrorMessage = "Channel must be between 0 and 31")]
    public int? Channel { get; set; }

    [RequiredForPhysicalSensor]
    public double? MinVoltage { get; set; }

    [RequiredForPhysicalSensor]
    public double? MaxVoltage { get; set; }

    [RequiredForPhysicalSensor]
    public double? MinValue { get; set; }

    [RequiredForPhysicalSensor]
    public double? MaxValue { get; set; }

    [Required]
    [GreaterThan(0, ErrorMessage = "SamplingRateMs must be greater than 0")]
    public int SamplingRateMs { get; set; } = 1000;

    [RequiredForVirtualSensor(ErrorMessage = "Virtual sensors must have a conversion expression to combine other sensor values")]
    public string? ConversionExpression { get; set; }
}