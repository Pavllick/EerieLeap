using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;

namespace ScriptInterpreter;

public class Interpreter : IDisposable {
    private bool _disposed = false;
    private IScriptEngine? _scriptEngine;
    private readonly HashSet<MethodInfo> _expectedMethods;

    public Interpreter(IEnumerable<MethodInfo> expectedMethods) =>
        _expectedMethods = new HashSet<MethodInfo>(expectedMethods);

    public Interpreter UpdateScript(string jsScript) {
        var validatedMethods = ValidateScript(jsScript);

        _scriptEngine?.Dispose();
        foreach (var methodInfo in _expectedMethods)
            methodInfo.Reset();

        _scriptEngine = new V8ScriptEngine();
        _scriptEngine.AddHostObject("console", new { log = new Action<string>(Console.WriteLine) });
        _scriptEngine.Evaluate(jsScript);

        foreach (var methodInfo in validatedMethods) {
            if(_expectedMethods.TryGetValue(methodInfo.MethodInfo, out var expectedMethodInfo))
                expectedMethodInfo.IsAvailable = methodInfo.IsAvailable;
        }

        EvaluateMethods();

        return this;
    }

    private object InvokeFunction(MethodInfo methodInfo, params object[] args) {
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

    private void EvaluateMethods() {
        if (_scriptEngine == null)
            throw new InvalidOperationException("Script engine is not initialized.");

        foreach (var methodInfo in _expectedMethods.Where(m => m.IsAvailable)) {
            var method = _scriptEngine.Script.GetProperty(methodInfo.Name) as ScriptObject;

            methodInfo.SetExcecuteHandler(method!.InvokeAsFunction);
        }
    }

    private IEnumerable<(MethodInfo MethodInfo, bool IsAvailable)> ValidateScript(string jsScript) {
        List<(MethodInfo MethodInfo, bool IsAvailable)> validatedMethods = new();

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
