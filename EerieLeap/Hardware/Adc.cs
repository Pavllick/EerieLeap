using Microsoft.Extensions.Logging;
using EerieLeap.Configuration;

namespace EerieLeap.Hardware;

public sealed class Adc : SpiAdc {
    public Adc(ILogger<Adc> logger) : base(logger) { }

    public override async Task<double> ReadChannelAsync(int channel, CancellationToken cancellationToken = default) =>
        await Task.Run(() => {
            var (_, rawValue) = TransferSpi(channel);
            var voltage = ConvertToVoltage(rawValue);
            LogReading(channel, rawValue, voltage);
            return voltage;
        }, cancellationToken).ConfigureAwait(false);
}
