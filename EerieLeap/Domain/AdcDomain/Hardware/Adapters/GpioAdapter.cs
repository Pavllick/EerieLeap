using System.Collections.ObjectModel;
using System.Device.Gpio;
using ScriptInterpreter;

namespace EerieLeap.Domain.AdcDomain.Hardware.Adapters;

public partial class GpioAdapter : IDisposable {
    private readonly ILogger _logger;
    private bool _isDisposed;

    internal static Collection<GpioAdapter> AllInstances { get; } = new();

    private readonly Interpreter _scriptInterpreter;
    private readonly GpioController _gpioController;
    private readonly Dictionary<int, List<CallbackInfo>> _pinChangeEventHandlers;

    private record CallbackInfo(string CallbackName, PinChangeEventHandler EventHandler);

    private static Timer _timer;

    private GpioAdapter(ILogger logger, Interpreter scriptInterpreter) {
        _logger = logger;
        _scriptInterpreter = scriptInterpreter;

        _gpioController = new GpioController(PinNumberingScheme.Logical);
        _pinChangeEventHandlers = new();
    }

    internal static GpioAdapter Create(ILogger logger, Interpreter scriptInterpreter) {
        var gpioAdapter = new GpioAdapter(logger, scriptInterpreter);
        AllInstances.Add(gpioAdapter);

        return gpioAdapter;
    }

    internal static Type[] GetTypesToRegister() => [
        typeof(GpioAdapter),
        typeof(PinEventTypes),
        typeof(PinMode),
        typeof(PinValue),
        typeof(PinValueChangedEventArgs),
        typeof(WaitForEventResult)
    ];

    /// <summary>
    /// Opens a pin and sets it to a specific mode and value.
    /// </summary>
    /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
    /// <param name="mode">The mode to be set.</param>
    /// <param name="initialValue">The initial value to be set if the mode is output. The driver will attempt to set the mode without causing glitches to the other value.
    /// (if <paramref name="initialValue"/> is <see cref="PinValue.High"/>, the pin should not glitch to low during open)</param>
    public GpioPin OpenPin(int pinNumber, PinMode mode, PinValue initialValue) =>
        _gpioController!.OpenPin(pinNumber, mode, initialValue);

    /// <summary>
    /// Closes an open pin.
    /// If allowed by the driver, the state of the pin is not changed.
    /// </summary>
    /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
    public void ClosePin(int pinNumber) =>
        _gpioController!.ClosePin(pinNumber);

    /// <summary>
    /// Writes a value to a pin.
    /// </summary>
    /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
    /// <param name="value">The value to be written to the pin.</param>
    public void Write(int pinNumber, PinValue pinValue) =>
        _gpioController!.Write(pinNumber, pinValue);

    /// <summary>
    /// Reads the current value of a pin.
    /// </summary>
    /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
    /// <returns>The value of the pin.</returns>
    public PinValue Read(int pinNumber) =>
        _gpioController!.Read(pinNumber);

    /// <summary>
    /// Toggle the current value of a pin.
    /// </summary>
    /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
    public void Toggle(int pinNumber) =>
        _gpioController!.Toggle(pinNumber);

    /// <summary>
    /// Checks if a specific pin is open.
    /// </summary>
    /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
    /// <returns>The status if the pin is open or closed.</returns>
    public bool IsPinOpen(int pinNumber) =>
        _gpioController!.IsPinOpen(pinNumber);

    /// <summary>
    /// Checks if a pin supports a specific mode.
    /// </summary>
    /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
    /// <param name="mode">The mode to check.</param>
    /// <returns>The status if the pin supports the mode.</returns>
    public bool IsPinModeSupported(int pinNumber, PinMode mode) =>
        _gpioController!.IsPinModeSupported(pinNumber, mode);

    /// <summary>
    /// Blocks execution until an event of type eventType is received or a period of time has expired.
    /// </summary>
    /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
    /// <param name="eventTypes">The event types to wait for.</param>
    /// <param name="timeoutMs">The time to wait for the event in milliseconds.</param>
    /// <returns>A structure that contains the result of the waiting operation.</returns>
    public WaitForEventResult WaitForEventMs(int pinNumber, PinEventTypes eventTypes, long timeoutMs) =>
        _gpioController!.WaitForEvent(pinNumber, eventTypes, TimeSpan.FromMilliseconds(timeoutMs));

    /// <summary>
    /// Adds a callback that will be invoked when pinNumber has an event of type eventType.
    /// </summary>
    /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
    /// <param name="eventTypes">The event types to wait for.</param>
    /// <param name="scriptCallbackFuncName">The script callback method name that will be invoked.</param>
    public void RegisterCallbackForPinValueChangedEvent(int pinNumber, PinEventTypes eventTypes, string scriptCallbackFuncName) {
        var callbackMethodInfo = new MethodInfo {
            Name = scriptCallbackFuncName,
            IsOptional = false,
            Parameters = [
                new ParameterInfo { Name = "sender", Type = typeof(object) },
                new ParameterInfo { Name = "pinValueChangedEventArgs", Type = typeof(PinValueChangedEventArgs) }],
        };
        _scriptInterpreter.AddMethod(callbackMethodInfo);

        var callbackHandler = new PinChangeEventHandler((sender, pinValueChangedEventArgs) =>
            callbackMethodInfo.Execute(sender, pinValueChangedEventArgs));

        _gpioController!.RegisterCallbackForPinValueChangedEvent(pinNumber, eventTypes, callbackHandler);

        if (_pinChangeEventHandlers.TryGetValue(pinNumber, out var handlers))
            handlers.Add(new(scriptCallbackFuncName, callbackHandler));
        else
            _pinChangeEventHandlers.Add(pinNumber, [new(scriptCallbackFuncName, callbackHandler)]);
    }

    protected virtual void Dispose(bool disposing) {
        if (_isDisposed)
            return;

        if (disposing) {
            _gpioController?.Dispose();

            foreach (var (pinNumber, handlerInfos) in _pinChangeEventHandlers) {
                foreach (var handlerInfo in handlerInfos)
                    _gpioController!.UnregisterCallbackForPinValueChangedEvent(pinNumber, handlerInfo.EventHandler);
            }

            _pinChangeEventHandlers.Clear();
        }

        _isDisposed = true;
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #region Loggers

    [LoggerMessage(Level = LogLevel.Information, Message = "GPIO Adapter created.")]
    private partial void LogAdapterCreated();

    #endregion
}
