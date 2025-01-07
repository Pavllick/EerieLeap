using EerieLeap.Configuration;
using EerieLeap.Domain.AdcDomain.Hardware.Adapters;
using ScriptInterpreter;
using System.ComponentModel.DataAnnotations;

namespace EerieLeap.Domain.AdcDomain.Hardware;

public partial class Adc : IAdc {
    private readonly ILogger _logger;
    private AdcConfig? _adcConfig;
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

        var hostTypes = SpiAdapter.GetTypesToRegister()
            .Concat(GpioAdapter.GetTypesToRegister());

        var hostObjects = new Dictionary<string, object>() {
            { nameof(SpiAdapter), new {
                Create = new Func<SpiAdapter>(() => SpiAdapter.Create(_logger, _adcConfig!))}},
            { nameof(GpioAdapter), new {
                Create = new Func<GpioAdapter>(() => GpioAdapter.Create(_logger, _scriptInterpreter!))}}};

        _scriptInterpreter = new Interpreter([_initMethodInfo, _processMethodInfo, _disposeMethodInfo], hostTypes, hostObjects);
    }

    public void UpdateConfiguration([Required] AdcConfig config) =>
        _adcConfig = config;

    public void UpdateProcessingScript([Required] string adcProcessScript) {
        foreach (var gpioAdapter in GpioAdapter.AllInstances)
            gpioAdapter.Dispose();
        GpioAdapter.AllInstances.Clear();

        foreach (var spiAdapter in SpiAdapter.AllInstances)
            spiAdapter.Dispose();
        SpiAdapter.AllInstances.Clear();

        _scriptInterpreter.UpdateScript(adcProcessScript);

        if (_initMethodInfo.IsAvailable) {
            try {
                _initMethodInfo.Execute();
            } catch(Exception ex) {
                LogExecuteScriptError(_initMethodInfo.Name, ex);
            }
        }
    }

    public async Task<double> ReadChannelAsync(int channel, CancellationToken cancellationToken = default) {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_adcConfig == null)
            throw new InvalidOperationException("ADC not configured. Call Configure first.");

        return await Task.Run(() => _processMethodInfo.Execute(channel), cancellationToken).ConfigureAwait(false);
    }


    public void Dispose() {
        if (_isDisposed)
            return;

        if (_disposeMethodInfo.IsAvailable) {
            try {
                _disposeMethodInfo.Execute();
            } catch (Exception ex) {
                LogExecuteScriptError(_disposeMethodInfo.Name, ex);
            }
        }

        _scriptInterpreter.Dispose();

        _isDisposed = true;
    }

    #region Loggers

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to execute script method '{scriptMethodName}'.")]
    private partial void LogExecuteScriptError(string scriptMethodName, Exception ex);

    #endregion
}
