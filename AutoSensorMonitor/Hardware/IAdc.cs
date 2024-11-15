namespace AutoSensorMonitor.Service.Hardware;

public interface IAdc : IDisposable 
{
    Task<double> ReadChannelAsync(int channel);
    void Configure(Hardware.AdcConfig config);
}
