using System.ComponentModel.DataAnnotations;
using System.Device.Spi;
using EerieLeap.Utilities.DataAnnotations;

namespace EerieLeap.Configuration;

[IgnoreCustomValidation]
public class AdcConfig {
    [Required]
    [MinLength(1)]
    public string? Type { get; set; }

    [Required]
    public int? BusId { get; set; }

    [Required]
    public int? ChipSelect { get; set; }

    [Required]
    public int? ClockFrequency { get; set; }

    [Required]
    public SpiMode? Mode { get; set; }

    [Required]
    public int? DataBitLength { get; set; }

    [Required]
    public int? Resolution { get; set; }

    [Required]
    public double? ReferenceVoltage { get; set; }

    [Required]
    public AdcProtocolConfig? Protocol { get; set; }
}
