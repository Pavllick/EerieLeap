using System.Device.Spi;
using Microsoft.Extensions.Logging;
using AutoSensorMonitor.Configuration;

namespace AutoSensorMonitor.Service.Hardware;

/// <summary>
/// Base class for SPI-based ADC implementations
/// </summary>
public abstract class SpiAdc : IAdc, IDisposable
{
    private readonly ILogger _logger;
    private SpiDevice? _spiDevice;
    private AdcConfig? _config;
    private bool _isDisposed;

    protected SpiAdc(ILogger logger)
    {
        _logger = logger;
    }

    public void Configure(AdcConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var settings = new SpiConnectionSettings(config.BusId, config.ChipSelect)
        {
            ClockFrequency = config.ClockFrequency,
            Mode = config.Mode,
            DataBitLength = config.DataBitLength
        };

        _spiDevice?.Dispose();
        _spiDevice = SpiDevice.Create(settings);
        _config = config;

        _logger.LogInformation(
            "Configured ADC: Type={Type}, Bus={Bus}, CS={CS}, Clock={Clock}Hz, Mode={Mode}, Resolution={Resolution}bit",
            config.Type,
            config.BusId,
            config.ChipSelect,
            config.ClockFrequency,
            config.Mode,
            config.Resolution
        );

        _logger.LogDebug(
            "ADC Protocol: CommandPrefix={Prefix}, ChannelMask={ChMask}, ResultMask={ResMask}, ReadBytes={Bytes}",
            BitConverter.ToString(config.Protocol.CommandPrefix),
            Convert.ToString(config.Protocol.ChannelMask, 2).PadLeft(8, '0'),
            Convert.ToString(config.Protocol.ResultBitMask, 2),
            config.Protocol.ReadByteCount
        );
    }

    public abstract Task<double> ReadChannelAsync(int channel, CancellationToken cancellationToken = default);

    protected (byte[] readBuffer, int rawValue) TransferSpi(int channel)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        
        if (_spiDevice == null || _config == null)
        {
            throw new InvalidOperationException("ADC not configured. Call Configure first.");
        }

        // Prepare command bytes using the protocol configuration
        byte commandByte = (byte)(_config.Protocol.CommandPrefix.FirstOrDefault() | 
            ((channel & _config.Protocol.ChannelMask) << _config.Protocol.ChannelBitShift));

        byte[] writeBuffer = new[] { commandByte };
        byte[] readBuffer = new byte[_config.Protocol.ReadByteCount];

        _spiDevice.TransferFullDuplex(writeBuffer, readBuffer);

        // Extract reading using configured bit masks and shifts
        int rawValue = 0;
        for (int i = 0; i < readBuffer.Length; i++)
        {
            rawValue = (rawValue << 8) | readBuffer[i];
        }

        rawValue = (rawValue >> _config.Protocol.ResultBitShift) & _config.Protocol.ResultBitMask;
        return (readBuffer, rawValue);
    }

    protected double ConvertToVoltage(int rawValue)
    {
        if (_config == null)
        {
            throw new InvalidOperationException("ADC not configured. Call Configure first.");
        }

        return (rawValue * _config.ReferenceVoltage) / ((1 << _config.Resolution) - 1);
    }

    protected void LogReading(int channel, int rawValue, double voltage)
    {
        _logger.LogTrace(
            "Channel {Channel}: Raw={Raw}, Voltage={Voltage:F3}V",
            channel,
            rawValue,
            voltage
        );
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        
        _spiDevice?.Dispose();
        _spiDevice = null;
        _config = null;
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }
}
