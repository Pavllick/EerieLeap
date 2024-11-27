using System.ComponentModel.DataAnnotations;
using System.Device.Spi;
using EerieLeap.Configuration;

namespace EerieLeap.Hardware;

/// <summary>
/// Base class for SPI-based ADC implementations
/// </summary>
public partial class SpiAdc : IAdc, IDisposable {
    private readonly ILogger _logger;
    private SpiDevice? _spiDevice;
    private AdcConfig? _config;
    private bool _isDisposed;

    public SpiAdc(ILogger logger) =>
        _logger = logger;

    public void Configure([Required] AdcConfig config) {
        var settings = new SpiConnectionSettings(config.BusId!.Value, config.ChipSelect!.Value) {
            ClockFrequency = config.ClockFrequency!.Value,
            Mode = config.Mode!.Value,
            DataBitLength = config.DataBitLength!.Value
        };

        _spiDevice?.Dispose();
        _spiDevice = SpiDevice.Create(settings);
        _config = config;

        LogConfiguration(
            config.Type!,
            config.BusId!.Value,
            config.ChipSelect!.Value,
            config.ClockFrequency!.Value,
            (int)config.Mode!.Value,
            config.Resolution!.Value);

        LogProtocol(
            BitConverter.ToString(config.Protocol!.CommandPrefix!),
            Convert.ToString(config.Protocol.ChannelMask!.Value, 2).PadLeft(8, '0'),
            Convert.ToString(config.Protocol.ResultBitMask!.Value, 2),
            config.Protocol.ReadByteCount!.Value);
    }

    public async Task<double> ReadChannelAsync(int channel, CancellationToken cancellationToken = default) =>
        await Task.Run(() => {
            var (_, rawValue) = TransferSpi(channel);
            var voltage = ConvertToVoltage(rawValue);
            LogChannelReading(channel, rawValue, voltage);
            return voltage;
        }, cancellationToken).ConfigureAwait(false);

    private (byte[] readBuffer, int rawValue) TransferSpi(int channel) {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_spiDevice == null || _config == null)
            throw new InvalidOperationException("ADC not configured. Call Configure first.");

        // Prepare command bytes using the protocol configuration
        var commandByte = (byte)(_config.Protocol!.CommandPrefix!.FirstOrDefault() |
            ((channel & _config.Protocol!.ChannelMask!.Value) << _config.Protocol!.ChannelBitShift!.Value));

        var writeBuffer = new[] { commandByte };
        var readBuffer = new byte[_config.Protocol!.ReadByteCount!.Value];

        _spiDevice.TransferFullDuplex(writeBuffer, readBuffer);

        // Extract reading using configured bit masks and shifts
        int rawValue = 0;
        for (int i = 0; i < readBuffer.Length; i++)
            rawValue = (rawValue << 8) | readBuffer[i];

        rawValue = (rawValue >> _config.Protocol!.ResultBitShift!.Value) & _config.Protocol!.ResultBitMask!.Value;
        return (readBuffer, rawValue);
    }

    private double ConvertToVoltage(int rawValue) =>
        _config == null
            ? throw new InvalidOperationException("ADC not configured. Call Configure first.")
            : (rawValue * _config.ReferenceVoltage!.Value) / ((1 << _config.Resolution!.Value) - 1);

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
