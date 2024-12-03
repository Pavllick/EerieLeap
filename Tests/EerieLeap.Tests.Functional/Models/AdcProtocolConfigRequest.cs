using System.Text.Json.Serialization;
using EerieLeap.Utilities.Converters;

namespace EerieLeap.Tests.Functional.Models;

/// <summary>
/// Test request model that mirrors AdcProtocolConfig for testing validation scenarios.
/// </summary>
public record AdcProtocolConfigRequest {
    [JsonConverter(typeof(HexByteArrayJsonConverter))]
    public byte[]? CommandPrefix { get; init; }

    [JsonConverter(typeof(HexNumberJsonConverter<byte>))]
    public byte? ChannelMask { get; init; }

    public int? ChannelBitShift { get; init; }

    [JsonConverter(typeof(HexNumberJsonConverter<int>))]
    public int? ResultBitMask { get; init; }

    public int? ResultBitShift { get; init; }

    public int? ReadByteCount { get; init; }

    /// <summary>
    /// Creates a valid ADC protocol configuration request.
    /// </summary>
    public static AdcProtocolConfigRequest CreateValid() => new() {
        CommandPrefix = new byte[] { 0x40 },
        ChannelMask = 0x0F,
        ChannelBitShift = 2,
        ResultBitMask = 0xFFF,
        ResultBitShift = 0,
        ReadByteCount = 2
    };
}
