using System.ComponentModel.DataAnnotations;
using System.Device.Gpio;
using System.Device.Spi;
using System.Diagnostics;
using EerieLeap.Configuration;

namespace EerieLeap.Domain.AdcDomain.Hardware;

/// <summary>
/// Base class for SPI-based ADC implementations
/// </summary>
public partial class SpiAdc : IAdc, IDisposable {
    private readonly ILogger _logger;
    private SpiDevice? _spiDevice;
    private GpioController _gpioController;
    private int _drdyPinNumber;
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

        _drdyPinNumber = 1;
        _gpioController = new GpioController();
        _gpioController.OpenPin(_drdyPinNumber, PinMode.Input);

        InitAdc();

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

        void InitAdc() {
            // Stop Read Data Continuously
            _spiDevice.Write([0x0f]); // SDATAC
            Thread.Sleep(10);
        }
    }

    public async Task<double> ReadChannelAsync(int channel, CancellationToken cancellationToken = default) =>
        await Task.Run(() => {
            //var (_, rawValue) = TransferSpi(channel);
            int rawValue = TransferSpiADS1256(channel);

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

    public bool IsDataReady() =>
        _gpioController.Read(_drdyPinNumber) == PinValue.Low;

    private int TransferSpiADS1256(int channel) {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_spiDevice == null || _config == null)
            throw new InvalidOperationException("ADC not configured. Call Configure first.");

        SetMultiplexer();

        var data = ReadData() ?? throw new InvalidOperationException("Data not ready.");

        // Extract reading using configured bit masks and shifts
        int rawValue = 0;
        for (int i = 0; i < data.Length; i++)
            rawValue = (rawValue << 8) | data[i];

        return rawValue;

        void SetMultiplexer() {
            var setMultiplexerCommand = new byte[] { 0x51, 0x00, (byte)((channel << 4) | 0b1000) };
            _spiDevice.Write(setMultiplexerCommand);
            Thread.Sleep(10);
        }

        byte[] ReadData() {
            int readTimeoutMs = 1000;

            Stopwatch sw = new();
            sw.Start();

            while (!IsDataReady() && sw.ElapsedMilliseconds < readTimeoutMs)
                Thread.Sleep(10);

            sw.Stop();

            if (!IsDataReady())
                return null;

            byte[] response = new byte[3];
            _spiDevice.TransferFullDuplex([0x01], response); // RDATA

            return response;
        }
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
            _gpioController.ClosePin(_drdyPinNumber);
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

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Configured ADC: Type={type}, Bus={bus}, CS={cs}, Clock={clock}Hz, Mode={mode}, Resolution={resolution}bit")]
    private partial void LogConfiguration(
        string type, int bus, int cs, int clock, int mode, int resolution);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "ADC Protocol: CommandPrefix={prefix}, ChannelMask={channelMask}, ResultMask={resultMask}, ReadBytes={readBytes}")]
    private partial void LogProtocol(
        string prefix, string channelMask, string resultMask, int readBytes);

    [LoggerMessage(Level = LogLevel.Trace,
        Message = "Channel {channel}: Raw={raw}, Voltage={voltage:F3}V")]
    private partial void LogChannelReading(int channel, int raw, double voltage);

    #endregion
}
