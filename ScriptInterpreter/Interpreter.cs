using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;

namespace ScriptInterpreter;

public class Interpreter : IDisposable {
    private bool _disposed = false;
    private IScriptEngine? _scriptEngine;
    private readonly HashSet<Type> _hostTypes;
    private readonly Dictionary<string, object> _hostObjects;
    private readonly HashSet<IMethodInfo> _expectedMethods;
    private readonly HashSet<IMethodInfo> _addedMethods;

    public event Action<IMethodInfo> MethodRemoved;

    public Interpreter(IEnumerable<IMethodInfo> expectedMethods, IEnumerable<Type>? hostTypes = null, IDictionary<string, object>? hostObjects = null) {
        _expectedMethods = new(expectedMethods);
        _addedMethods = new();

        _hostTypes = new HashSet<Type>();

        _hostObjects = new Dictionary<string, object>() {
            { "console", new { log = new Action<string>(Console.WriteLine) } }
        };

        if (hostTypes != null) {
            foreach (var type in hostTypes)
                _hostTypes.Add(type);
        }

        if (hostObjects != null) {
            foreach (var (key, value) in hostObjects)
                _hostObjects.Add(key, value);
        }
    }

    public Interpreter UpdateScript(string jsScript) {
        var validatedMethods = ValidateScript(jsScript);

        _scriptEngine?.Dispose();
        ResetMethods(_expectedMethods);
        ResetMethods(_addedMethods);

        _scriptEngine = new V8ScriptEngine();

        foreach (var type in _hostTypes)
            _scriptEngine.AddHostType(type.Name, type);

        foreach (var (key, value) in _hostObjects)
            _scriptEngine.AddHostObject(key, value);

        _scriptEngine.Evaluate(jsScript);

        foreach (var expectedMethodInfo in _expectedMethods) {
            if (validatedMethods.Any(vm => vm.MethodInfo == expectedMethodInfo && vm.IsAvailable))
                expectedMethodInfo.IsAvailable = true;
        }

        foreach (var addedMethod in _addedMethods.ToArray()) {
            if (validatedMethods.Any(vm => vm.MethodInfo == addedMethod && vm.IsAvailable)) {
                addedMethod.IsAvailable = true;
            } else {
                MethodRemoved?.Invoke(addedMethod);
                _addedMethods.Remove(addedMethod);
            }
        }

        MapMethods();

        return this;

        static void ResetMethods(IEnumerable<IMethodInfo> methodInfos) {
            foreach (var methodInfo in methodInfos)
                methodInfo.Reset();
        }
    }

    private object InvokeFunction(IMethodInfo methodInfo, params object[] args) {
        if (_scriptEngine == null)
            throw new InvalidOperationException("Script engine is not initialized.");

        if (!methodInfo.IsAvailable)
            throw new InvalidOperationException($"Function '{methodInfo.Name}' is not available in the script.");

        try {
            if (_scriptEngine.Script.GetProperty(methodInfo.Name) is ScriptObject scriptObject && scriptObject != null)
                return scriptObject.InvokeAsFunction(args);
            else
                throw new MissingMethodException($"Function '{methodInfo.Name}' is not defined in the script.");
        } catch (Exception ex) {
            throw new InvalidOperationException($"Error invoking '{methodInfo.Name}': {ex.Message}", ex);
        }
    }

    public void AddMethod(IMethodInfo methodInfo) {
        MapMethod(methodInfo);

        _addedMethods.Add(methodInfo);
    }

    private void MapMethods() {
        foreach (var methodInfo in _expectedMethods.Where(m => m.IsAvailable))
            MapMethod(methodInfo);

        foreach (var methodInfo in _addedMethods.Where(m => m.IsAvailable))
            MapMethod(methodInfo);
    }

    private void MapMethod(IMethodInfo methodInfo) {
        if (_scriptEngine == null)
            throw new InvalidOperationException("Script engine is not initialized.");

        var method = _scriptEngine.Script.GetProperty(methodInfo.Name) as ScriptObject;
        methodInfo.SetExcecuteHandler(method!.InvokeAsFunction);
    }

    private IEnumerable<(IMethodInfo MethodInfo, bool IsAvailable)> ValidateScript(string jsScript) {
        List<(IMethodInfo MethodInfo, bool IsAvailable)> validatedMethods = new();

        using var scriptEngine = new V8ScriptEngine();

        dynamic IsFunctionObject = scriptEngine.Evaluate("obj => obj instanceof Function");
        var IsFunction = (object value) => value is ScriptObject obj && IsFunctionObject(obj);

        scriptEngine.Evaluate(jsScript);


        foreach (var methodInfo in _expectedMethods) {
            var v = IsFunction(scriptEngine.Script.GetProperty(methodInfo.Name));

            if (scriptEngine.Script.GetProperty(methodInfo.Name) is ScriptObject scriptObject && scriptObject != null && IsFunction(scriptObject))
                validatedMethods.Add((methodInfo, true));
            else if (!methodInfo.IsOptional)
                throw new InvalidOperationException($"The script must implement the '{methodInfo.Name}' function.");
        }

        foreach (var methodInfo in _addedMethods) {
            var v = IsFunction(scriptEngine.Script.GetProperty(methodInfo.Name));

            if (scriptEngine.Script.GetProperty(methodInfo.Name) is ScriptObject scriptObject && scriptObject != null && IsFunction(scriptObject))
                validatedMethods.Add((methodInfo, true));
        }

        return validatedMethods;
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) {
        if (!_disposed) {
            if (disposing)
                _scriptEngine?.Dispose();

            _disposed = true;
        }
    }
}
