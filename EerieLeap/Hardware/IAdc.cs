using EerieLeap.Configuration;
using System.Threading;

namespace EerieLeap.Hardware;

public interface IAdc : IDisposable 
{
    Task<double> ReadChannelAsync(int channel, CancellationToken cancellationToken = default);
    void Configure(AdcConfig config);
}
