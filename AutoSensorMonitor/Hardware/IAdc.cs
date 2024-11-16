using AutoSensorMonitor.Configuration;
using System.Threading;

namespace AutoSensorMonitor.Hardware;

public interface IAdc : IDisposable 
{
    Task<double> ReadChannelAsync(int channel, CancellationToken cancellationToken = default);
    void Configure(AdcConfig config);
}
