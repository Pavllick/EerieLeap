using EerieLeap.Configuration;

namespace EerieLeap.Domain.AdcDomain.Hardware;

public interface IAdc : IDisposable {
    void Configure(AdcConfig config);
    Task<double> ReadChannelAsync(int channel, CancellationToken cancellationToken = default);
}
