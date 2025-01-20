using System.ComponentModel.DataAnnotations;
using System.Device.Spi;
using EerieLeap.Configuration;
using Microsoft.ClearScript.JavaScript;

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
    /// <param name="bytesToRead">Number of bytes to read the data from the SPI device.</param>
    /// <returns>Byte array read from the SPI device.</returns>
    public byte[] Read([Required] int bytesToRead) {
        Span<byte> readBuff = stackalloc byte[bytesToRead];
        _spiDevice!.Read(readBuff);

        return readBuff.ToArray();
    }

    /// <summary>
    /// Writes a byte to the SPI device.
    /// </summary>
    /// <param name="value">The byte to be written to the SPI device.</param>
    public void WriteByte([Required] byte value) =>
        _spiDevice!.WriteByte(value);

    /// <summary>
    /// Writes data to the SPI device.
    /// </summary>
    /// <param name="buffer">
    /// The buffer that contains the data to be written to the SPI device.
    /// </param>
    public void Write([Required] ITypedArray<byte> buffer) =>
        _spiDevice!.Write(buffer.ToArray());

    /// <summary>
    /// Writes and reads data from the SPI device.
    /// </summary>
    /// <param name="writeBuffer">The buffer that contains the data to be written to the SPI device.</param>
    /// <param name="bytesToRead">Number of bytes to read the data from the SPI device.</param>
    public byte[] TransferFullDuplex([Required] ITypedArray<byte> writeBuffer, [Required] int bytesToRead) {
        Span<byte> readBuff = stackalloc byte[bytesToRead];
        _spiDevice!.TransferFullDuplex(writeBuffer.ToArray(), readBuff);

        return readBuff.ToArray();
    }

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
