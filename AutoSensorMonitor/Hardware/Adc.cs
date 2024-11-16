using Microsoft.Extensions.Logging;
using AutoSensorMonitor.Configuration;

namespace AutoSensorMonitor.Hardware;

public sealed class Adc : SpiAdc
{
    public Adc(ILogger<Adc> logger) : base(logger)
    {}

    public override async Task<double> ReadChannelAsync(int channel, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var (_, rawValue) = TransferSpi(channel);
            var voltage = ConvertToVoltage(rawValue);
            LogReading(channel, rawValue, voltage);
            return voltage;
        }, cancellationToken).ConfigureAwait(false);
    }
}
