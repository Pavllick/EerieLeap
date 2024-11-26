using EerieLeap.Configuration;
using System.Threading;

namespace EerieLeap.Hardware;

public interface IAdc : IDisposable {
    void Configure(AdcConfig config);
    Task<double> ReadChannelAsync(int channel, CancellationToken cancellationToken = default);
}
