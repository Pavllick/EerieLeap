using System.Device.Spi;

namespace AutoSensorMonitor.Service.Hardware;

public sealed class Ads7953 : IAdc {
    private SpiDevice? _spiDevice;
    private bool _isDisposed;
    
    public void Configure(AdcConfig config) {
        ArgumentNullException.ThrowIfNull(config);

        var settings = new SpiConnectionSettings(config.BusId, config.ChipSelect) {
            ClockFrequency = config.ClockFrequency,
            Mode = SpiMode.Mode0,
            DataBitLength = 8
        };

        _spiDevice?.Dispose();
        _spiDevice = SpiDevice.Create(settings);
    }

    public async Task<double> ReadChannelAsync(int channel) {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_spiDevice == null) {
            throw new InvalidOperationException("ADC not configured. Call Configure first.");
        }

        return await Task.Run(() => {
            // ADS7953 command byte format:
            // [7:6] = 01 (Manual channel selection)
            // [5:2] = Channel number
            // [1:0] = 00 (Don't care)
            byte commandByte = (byte)(0x40 | (channel << 2));

            byte[] writeBuffer = new byte[] { commandByte };
            byte[] readBuffer = new byte[2];

            _spiDevice.TransferFullDuplex(writeBuffer, readBuffer);

            // Convert the 12-bit result to voltage
            int rawValue = ((readBuffer[0] & 0x0F) << 8) | readBuffer[1];
            const double referenceVoltage = 3.3;
            const int maxAdcValue = 4096;
            
            return (rawValue * referenceVoltage) / maxAdcValue;
        }).ConfigureAwait(false);
    }

    public void Dispose() {
        if (_isDisposed) {
            return;
        }

        _spiDevice?.Dispose();
        _spiDevice = null;
        _isDisposed = true;
    }
}
