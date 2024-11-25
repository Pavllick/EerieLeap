using System.Device.Spi;

namespace EerieLeap.Tests.Functional.Models;

/// <summary>
/// Test request model that mirrors AdcConfig for testing validation scenarios.
/// </summary>
public record AdcConfigRequest {
    public string? Type { get; init; }
    public int? BusId { get; init; }
    public int? ChipSelect { get; init; }
    public int? ClockFrequency { get; init; }
    public SpiMode? Mode { get; init; }
    public int? DataBitLength { get; init; }
    public int? Resolution { get; init; }
    public double? ReferenceVoltage { get; init; }
    public AdcProtocolConfigRequest? Protocol { get; init; }

    /// <summary>
    /// Creates a valid ADC configuration request.
    /// </summary>
    public static AdcConfigRequest CreateValid() => new() {
        Type = "ADS7953",
        BusId = 0,
        ChipSelect = 0,
        ClockFrequency = 1_000_000,
        Mode = SpiMode.Mode0,
        DataBitLength = 8,
        Resolution = 12,
        ReferenceVoltage = 3.3,
        Protocol = AdcProtocolConfigRequest.CreateValid()
    };
}
