using System.ComponentModel.DataAnnotations;

namespace EerieLeap.Configuration;

public class AdcProtocolConfig
{
    [Required]
    public byte[] CommandPrefix { get; set; } = new byte[] { 0x01 };

    [Required]
    public byte ChannelMask { get; set; } = 0x70;

    [Required]
    public int ChannelBitShift { get; set; } = 4;

    [Required]
    public int ResultBitMask { get; set; } = 0x3FF;

    [Required]
    public int ResultBitShift { get; set; } = 0;

    [Required]
    public int ReadByteCount { get; set; } = 3;
}
