using System;
using System.Buffers;
using System.Device.Spi;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using EerieLeap.Configuration;

namespace EerieLeap.Hardware;

/// <summary>
/// Base class for SPI-based ADC implementations
/// </summary>
public abstract partial class SpiAdc : IAdc, IDisposable {
    private readonly ILogger _logger;
    private SpiDevice? _spiDevice;
    private AdcConfig? _config;
    private bool _isDisposed;

    protected SpiAdc(ILogger logger) {
        _logger = logger;
    }

    public void Configure(AdcConfig config) {
        ArgumentNullException.ThrowIfNull(config);

        var settings = new SpiConnectionSettings(config.BusId, config.ChipSelect) {
            ClockFrequency = config.ClockFrequency,
            Mode = config.Mode,
            DataBitLength = config.DataBitLength
        };

        _spiDevice?.Dispose();
        _spiDevice = SpiDevice.Create(settings);
        _config = config;

        LogConfiguration(
            config.Type,
            config.BusId,
            config.ChipSelect,
            config.ClockFrequency,
            (int)config.Mode,
            config.Resolution);

        LogProtocol(
            BitConverter.ToString(config.Protocol.CommandPrefix),
            Convert.ToString(config.Protocol.ChannelMask, 2).PadLeft(8, '0'),
            Convert.ToString(config.Protocol.ResultBitMask, 2),
            config.Protocol.ReadByteCount);
    }

    public abstract Task<double> ReadChannelAsync(int channel, CancellationToken cancellationToken = default);

    protected (byte[] readBuffer, int rawValue) TransferSpi(int channel) {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_spiDevice == null || _config == null)
            throw new InvalidOperationException("ADC not configured. Call Configure first.");

        // Prepare command bytes using the protocol configuration
        byte commandByte = (byte)(_config.Protocol.CommandPrefix.FirstOrDefault() |
            ((channel & _config.Protocol.ChannelMask) << _config.Protocol.ChannelBitShift));

        byte[] writeBuffer = new[] { commandByte };
        byte[] readBuffer = new byte[_config.Protocol.ReadByteCount];

        _spiDevice.TransferFullDuplex(writeBuffer, readBuffer);

        // Extract reading using configured bit masks and shifts
        int rawValue = 0;
        for (int i = 0; i < readBuffer.Length; i++)
            rawValue = (rawValue << 8) | readBuffer[i];

        rawValue = (rawValue >> _config.Protocol.ResultBitShift) & _config.Protocol.ResultBitMask;
        return (readBuffer, rawValue);
    }

    protected double ConvertToVoltage(int rawValue) {
        if (_config == null)
            throw new InvalidOperationException("ADC not configured. Call Configure first.");

        return (rawValue * _config.ReferenceVoltage) / ((1 << _config.Resolution) - 1);
    }

    protected void LogReading(int channel, int rawValue, double voltage) {
        LogChannelReading(channel, rawValue, voltage);
    }

    protected virtual void Dispose(bool disposing) {
        if (_isDisposed)
            return;

        if (disposing) {
            _spiDevice?.Dispose();
            _spiDevice = null;
            _config = null;
        }

        _isDisposed = true;
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #region Loggers

    [LoggerMessage(Level = LogLevel.Information, EventId = 1,
        Message = "Configured ADC: Type={type}, Bus={bus}, CS={cs}, Clock={clock}Hz, Mode={mode}, Resolution={resolution}bit")]
    private partial void LogConfiguration(
        string type, int bus, int cs, int clock, int mode, int resolution);

    [LoggerMessage(Level = LogLevel.Debug, EventId = 2,
        Message = "ADC Protocol: CommandPrefix={prefix}, ChannelMask={channelMask}, ResultMask={resultMask}, ReadBytes={readBytes}")]
    private partial void LogProtocol(
        string prefix, string channelMask, string resultMask, int readBytes);

    [LoggerMessage(Level = LogLevel.Trace, EventId = 3,
        Message = "Channel {channel}: Raw={raw}, Voltage={voltage:F3}V")]
    private partial void LogChannelReading(
        int channel, int raw, double voltage);

    #endregion
}
