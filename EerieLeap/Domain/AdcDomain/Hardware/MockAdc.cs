using EerieLeap.Configuration;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;

namespace EerieLeap.Domain.AdcDomain.Hardware;

public sealed class MockAdc : IAdc {
    private readonly RandomNumberGenerator _random = RandomNumberGenerator.Create();
    private AdcConfig? _config;
    private bool _isDisposed;
    private readonly Dictionary<int, double> _lastValues = new();
    private readonly Dictionary<int, double> _trends = new();

    public void Configure([Required] AdcConfig config) =>
        _config = config;

    public async Task<double> ReadChannelAsync(int channel, CancellationToken cancellationToken = default) {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_config == null)
            throw new InvalidOperationException("ADC not configured. Call Configure first.");

        return await Task.Run(() => {
            // Initialize trend for this channel if not exists
            if (!_trends.ContainsKey(channel))
                _trends[channel] = GetRandomDouble() * 2 - 1; // Random trend between -1 and 1

            // Initialize last value if not exists
            if (!_lastValues.ContainsKey(channel))
                _lastValues[channel] = GetRandomDouble() * 3.3; // Initial value between 0 and 3.3V

            // Randomly change trend sometimes
            if (GetRandomDouble() < 0.1) // 10% chance to change trend
                _trends[channel] = GetRandomDouble() * 2 - 1;

            // Calculate new value with some randomness and trend
            var currentValue = _lastValues[channel];
            var trend = _trends[channel];
            var maxChange = 0.1; // Maximum change per reading
            var change = (trend * 0.8 + GetRandomDouble() * 0.4 - 0.2) * maxChange;

            var newValue = currentValue + change;

            // Keep within ADC range (0 to 3.3V)
            newValue = Math.Max(0, Math.Min(3.3, newValue));

            // Store the new value
            _lastValues[channel] = newValue;

            return newValue;
        }, cancellationToken).ConfigureAwait(false);
    }

    private double GetRandomDouble() {
        var buffer = new byte[8];
        _random.GetBytes(buffer);

        return (double)BitConverter.ToUInt64(buffer, 0) / ulong.MaxValue;
    }

    public void Dispose() {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _random.Dispose();
    }
}
