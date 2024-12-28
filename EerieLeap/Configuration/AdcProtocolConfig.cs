using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using EerieLeap.Utilities.Converters;
using ValidationProcessor.DataAnnotations;

namespace EerieLeap.Configuration;

[IgnoreCustomValidation]
public class AdcProtocolConfig {
    [Required]
    [JsonConverter(typeof(HexByteArrayJsonConverter))]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Irrelevant for configuration")]
    public byte[]? CommandPrefix { get; set; }

    [Required]
    [JsonConverter(typeof(HexNumberJsonConverter<byte>))]
    public byte? ChannelMask { get; set; }

    [Required]
    public int? ChannelBitShift { get; set; }

    [Required]
    [JsonConverter(typeof(HexNumberJsonConverter<int>))]
    public int? ResultBitMask { get; set; }

    [Required]
    public int? ResultBitShift { get; set; }

    [Required]
    public int? ReadByteCount { get; set; }
}
