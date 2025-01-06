using EerieLeap.Configuration;

namespace EerieLeap.Domain.AdcDomain.Hardware;

public interface IAdc : IDisposable {
    void UpdateConfiguration(AdcConfig config);
    void UpdateProcessingScript(string adcProcessScript);
    Task<double> ReadChannelAsync(int channel, CancellationToken cancellationToken = default);
}
