using System.Text.Json;
using AutoSensorMonitor.Configuration;

namespace AutoSensorMonitor.Hardware;

public sealed class MockAdc : IAdc {
    private readonly Random _random = new();
    private AdcConfig? _config;
    private bool _isDisposed;

    public void Configure(AdcConfig config) {
        ArgumentNullException.ThrowIfNull(config);
        _config = config;
    }

    public async Task<double> ReadChannelAsync(int channel, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_config == null) {
            throw new InvalidOperationException("ADC not configured. Call Configure first.");
        }

        return await Task.Run(() => {
            return _random.NextDouble() * 3.3; // Mock voltage between 0 and 3.3V
        }, cancellationToken).ConfigureAwait(false);
    }

    public void Dispose() {
        if (_isDisposed) {
            return;
        }

        _isDisposed = true;
    }
}
