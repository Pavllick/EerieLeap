using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using EerieLeap.Utilities;

namespace EerieLeap.Configuration;

public class AdcProtocolConfig {
    [Required]
    [JsonConverter(typeof(HexByteArrayConverter))]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Irrelevant for configuration")]
    public byte[] CommandPrefix { get; set; } = new byte[] { 0x01 };

    [Required]
    [JsonConverter(typeof(HexNumberConverter<byte>))]
    public byte ChannelMask { get; set; } = 0x70;

    [Required]
    public int ChannelBitShift { get; set; } = 4;

    [Required]
    [JsonConverter(typeof(HexNumberConverter<int>))]
    public int ResultBitMask { get; set; } = 0x3FF;

    [Required]
    public int ResultBitShift { get; set; } = 0;

    [Required]
    public int ReadByteCount { get; set; } = 3;
}
