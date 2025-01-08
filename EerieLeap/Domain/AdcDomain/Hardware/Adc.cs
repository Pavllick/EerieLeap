using EerieLeap.Configuration;
using EerieLeap.Domain.AdcDomain.Hardware.Adapters;
using ScriptInterpreter;
using System.Collections.ObjectModel;
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

    private Collection<GpioAdapter> _gpioAdapters { get; } = new();
    private Collection<SpiAdapter> _spiAdapters { get; } = new();

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

        var gpioAdapterCreateCallback = new Func<GpioAdapter>(() => {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
                LogAdapterNotSupportedError("GPIO", "Windows");

                return null;
            }

            var gpioAdapter = GpioAdapter.Create(_logger, _scriptInterpreter!);
            _gpioAdapters.Add(gpioAdapter);

            return gpioAdapter;
        });

        var spiAdapterCreateCallback = new Func<SpiAdapter>(() => {
            var spiAdapter = SpiAdapter.Create(_logger, _adcConfig!);
            _spiAdapters.Add(spiAdapter);

            return spiAdapter;
        });

        var hostObjects = new Dictionary<string, object>() {
            { nameof(GpioAdapter), new { Create = gpioAdapterCreateCallback}},
            { nameof(SpiAdapter), new { Create = spiAdapterCreateCallback }}};

        _scriptInterpreter = new Interpreter([_initMethodInfo, _processMethodInfo, _disposeMethodInfo], hostTypes, hostObjects);
    }

    public void UpdateConfiguration([Required] AdcConfig config) =>
        _adcConfig = config;

    public void UpdateProcessingScript([Required] string adcProcessScript) {
        foreach (var gpioAdapter in _gpioAdapters)
            gpioAdapter.Dispose();
        _gpioAdapters.Clear();

        foreach (var spiAdapter in _spiAdapters)
            spiAdapter.Dispose();
        _spiAdapters.Clear();

        _scriptInterpreter.UpdateScript(adcProcessScript);

        if (_initMethodInfo.IsAvailable) {
            try {
                _initMethodInfo.Execute();
            } catch(Exception ex) {
                LogExecuteScriptError(_initMethodInfo.Name, ex.Message);
                LogExceptionDetails(ex);
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
                LogExecuteScriptError(_disposeMethodInfo.Name, ex.Message);
                LogExceptionDetails(ex);
            }
        }

        _scriptInterpreter.Dispose();

        _isDisposed = true;
    }

    #region Loggers

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to execute script method '{scriptMethodName}'. {exceptionMessage}")]
    private partial void LogExecuteScriptError(string scriptMethodName, string exceptionMessage);

    [LoggerMessage(Level = LogLevel.Error, Message = "{adapterType} adapter is not supported on {systemName}.")]
    private partial void LogAdapterNotSupportedError(string adapterType, string systemName);

    // Debug loggers

    [LoggerMessage(Level = LogLevel.Debug)]
    private partial void LogExceptionDetails(Exception ex);

    #endregion
}
