using EerieLeap.Configuration;
using ScriptInterpreter;
using System.ComponentModel.DataAnnotations;

namespace EerieLeap.Domain.AdcDomain.Hardware;

public sealed class Adc : IAdc {
    private readonly ILogger _logger;
    private AdcConfig? _config;
    private bool _isDisposed;

    private readonly Interpreter _scriptInterpreter;
    private readonly MethodInfo _initMethodInfo;
    private readonly MethodInfo<double> _processMethodInfo;
    private readonly MethodInfo _disposeMethodInfo;

    public Adc(ILogger logger) {
        _logger = logger;

        _initMethodInfo = new MethodInfo {
            Name = "init",
            IsOptional = true,
        };

        _processMethodInfo = new MethodInfo<double> {
            Name = "process",
            IsOptional = false,
            Parameters = [new ParameterInfo { Name = "channel", Type = typeof(int) }],
        };

        _disposeMethodInfo = new MethodInfo {
            Name = "dispose",
            IsOptional = true,
        };

        _scriptInterpreter = new Interpreter([_initMethodInfo, _processMethodInfo, _disposeMethodInfo]);
    }

    public void UpdateConfiguration([Required] AdcConfig config) =>
        _config = config;

    public void UpdateProcessingScript([Required] string adcProcessScript) {
        _scriptInterpreter.UpdateScript(adcProcessScript);

        if (_initMethodInfo.IsAvailable)
            _initMethodInfo.Execute();
    }

    public async Task<double> ReadChannelAsync(int channel, CancellationToken cancellationToken = default) {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_config == null)
            throw new InvalidOperationException("ADC not configured. Call Configure first.");

        return await Task.Run(() => _processMethodInfo.Execute(channel), cancellationToken).ConfigureAwait(false);
    }


    public void Dispose() {
        if (_isDisposed)
            return;

        if (_disposeMethodInfo.IsAvailable)
            _disposeMethodInfo.Execute();

        _scriptInterpreter.Dispose();

        _isDisposed = true;
    }
}
