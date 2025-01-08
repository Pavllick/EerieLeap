using System.ComponentModel.DataAnnotations;
using System.Device.Spi;
using EerieLeap.Configuration;

namespace EerieLeap.Domain.AdcDomain.Hardware.Adapters;

public partial class SpiAdapter : IDisposable {
    private readonly ILogger _logger;
    private bool _isDisposed;

    private readonly SpiDevice? _spiDevice;
    private readonly AdcConfig? _adcConfig;

    private SpiAdapter([Required] ILogger logger, [Required] AdcConfig adcConfig) {
        _logger = logger;
        _adcConfig = adcConfig;

        var settings = new SpiConnectionSettings(adcConfig.BusId!.Value, adcConfig.ChipSelect!.Value) {
            Mode = adcConfig.Mode!.Value,
            DataBitLength = adcConfig.DataBitLength!.Value,
            ClockFrequency = adcConfig.ClockFrequency!.Value,
        };

        LogAdapterCreated();
    }

    internal static SpiAdapter Create([Required] ILogger logger, [Required] AdcConfig adcConfig) =>
        new SpiAdapter(logger, adcConfig);

    internal static Type[] GetTypesToRegister() => [
        typeof(SpiAdapter)
    ];

    /// <summary>
    /// Reads a byte from the SPI device.
    /// </summary>
    /// <returns>A byte read from the SPI device.</returns>
    public byte ReadByte() =>
        _spiDevice!.ReadByte();

    /// <summary>
    /// Reads data from the SPI device.
    /// </summary>
    /// <param name="buffer">
    /// The buffer to read the data from the SPI device.
    /// The length of the buffer determines how much data to read from the SPI device.
    /// </param>
    public void Read(byte[] buffer) =>
        _spiDevice!.Read(buffer);

    /// <summary>
    /// Writes a byte to the SPI device.
    /// </summary>
    /// <param name="value">The byte to be written to the SPI device.</param>
    public void WriteByte(byte value) =>
        _spiDevice!.WriteByte(value);

    /// <summary>
    /// Writes data to the SPI device.
    /// </summary>
    /// <param name="buffer">
    /// The buffer that contains the data to be written to the SPI device.
    /// </param>
    public void Write(byte[] buffer) =>
        _spiDevice!.Write(buffer);

    /// <summary>
    /// Writes and reads data from the SPI device.
    /// </summary>
    /// <param name="writeBuffer">The buffer that contains the data to be written to the SPI device.</param>
    /// <param name="readBuffer">The buffer to read the data from the SPI device.</param>
    public void TransferFullDuplex(byte[] writeBuffer, byte[] readBuffer) =>
        _spiDevice!.TransferFullDuplex(writeBuffer, readBuffer);

    protected virtual void Dispose(bool disposing) {
        if (_isDisposed)
            return;

        if (disposing)
            _spiDevice?.Dispose();

        _isDisposed = true;
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #region Loggers

    [LoggerMessage(Level = LogLevel.Information, Message = "SPI Adapter created.")]
    private partial void LogAdapterCreated();

    #endregion
}
