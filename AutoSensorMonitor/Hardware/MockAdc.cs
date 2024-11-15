using System.Text.Json;

namespace AutoSensorMonitor.Service.Hardware;

public sealed class MockAdc : IAdc {
    private readonly Random _random = new();
    private AdcConfig? _config;
    private bool _isDisposed;

    public void Configure(AdcConfig config) {
        ArgumentNullException.ThrowIfNull(config);
        _config = config;
    }

    public Task<double> ReadChannelAsync(int channel) {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_config == null) {
            throw new InvalidOperationException("ADC not configured. Call Configure first.");
        }

        // Generate a random voltage between 0.5V and 4.5V
        var voltage = _random.NextDouble() * 4.0 + 0.5;
        return Task.FromResult(voltage);
    }

    public void Dispose() {
        if (_isDisposed) {
            return;
        }

        _isDisposed = true;
    }
}
