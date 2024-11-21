using System.ComponentModel.DataAnnotations;
using System.Device.Spi;

namespace EerieLeap.Configuration;

public class AdcConfig {
    [Required]
    public string Type { get; set; } = string.Empty;

    [Required]
    public int BusId { get; set; }

    [Required]
    public int ChipSelect { get; set; }

    [Required]
    public int ClockFrequency { get; set; } = 1_000_000; // Default to 1MHz

    [Required]
    public SpiMode Mode { get; set; } = SpiMode.Mode0;

    [Required]
    public int DataBitLength { get; set; } = 8;

    [Required]
    public int Resolution { get; set; } = 10; // Default to 10-bit resolution

    [Required]
    public double ReferenceVoltage { get; set; } = 3.3; // Default to 3.3V

    [Required]
    public AdcProtocolConfig Protocol { get; set; } = new();
}
