using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using EerieLeap.Utilities.Converters;

namespace EerieLeap.Configuration;

public class AdcProtocolConfig {
    [Required]
    [JsonConverter(typeof(HexByteArrayConverter))]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Irrelevant for configuration")]
    public byte[]? CommandPrefix { get; set; }

    [Required]
    [JsonConverter(typeof(HexNumberConverter<byte>))]
    public byte? ChannelMask { get; set; }

    [Required]
    public int? ChannelBitShift { get; set; }

    [Required]
    [JsonConverter(typeof(HexNumberConverter<int>))]
    public int? ResultBitMask { get; set; }

    [Required]
    public int? ResultBitShift { get; set; }

    [Required]
    public int? ReadByteCount { get; set; }
}
