using ScriptInterpreter.Utilities;

namespace ScriptInterpreter;

public class MethodInfo : IMethodInfo {
    public required string Name { get; set; }
    public bool IsOptional { get; set; }
    public bool IsAvailable { get; set; }
    public ParameterInfo[] Parameters { get; set; } = Array.Empty<ParameterInfo>();

    private Func<object[], object>? ExcecuteHandler { get; set; }

    public object Execute(params object[] args) {
        if (ExcecuteHandler == null)
            throw new InvalidOperationException($"Function '{Name}' is not available in the script.");

        if (Parameters.Select(p => !p.IsOptional).Count() > args.Length || Parameters.Length < args.Length)
            throw new ArgumentException($"Function '{Name}' requires {Parameters.Length} parameters.");

        for (int i = 0; i < args.Length; i++) {
            if (!Parameters[i].Type.IsInstanceOfType(args[i]))
                throw new ArgumentException($"Parameter '{Parameters[i].Name}' must be of type '{Parameters[i].Type.Name}'.");
        }

        return ExcecuteHandler(args);
    }

    public T Execute<T>(params object[] args) {
        var result = Execute(args);

        if (!TypeConverter.TryConvert(result, out T value))
            throw new InvalidOperationException($"Cannot convert result of type '{result.GetType().Name}' to '{typeof(T).Name}'.");

        return value;
    }

    public void SetExcecuteHandler(Func<object[], object> handler) {
        ExcecuteHandler = handler;
        IsAvailable = true;
    }

    public void Reset() {
        IsAvailable = false;
        ExcecuteHandler = null;
    }
}

public class MethodInfo<T> : MethodInfo {
    public new T Execute(params object[] args) =>
        Execute<T>(args);
}

