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
            throw new InvalidOperationException($"Function '{Name}' requires {Parameters.Length} parameters.");

        for (int i = 0; i < args.Length; i++) {
            if (!Parameters[i].Type.IsInstanceOfType(args[i]))
                throw new InvalidOperationException($"Parameter '{Parameters[i].Name}' must be of type '{Parameters[i].Type.Name}'.");
        }

        return ExcecuteHandler(args);
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
    public new T Execute(params object[] args) {
        var result = base.Execute(args);

        if (!Converter.TryConvert(result, out T value))
            throw new InvalidOperationException($"Cannot convert result of type '{result.GetType().Name}' to '{typeof(T).Name}'.");

        return value;
    }
}

public static class Converter {
    public static bool TryConvert<T>(object input, out T result) {
        try {
            if (input is T typedInput) {
                result = typedInput;
                return true;
            }

            if (typeof(T).IsPrimitive || typeof(T) == typeof(string)) {
                result = (T)Convert.ChangeType(input, typeof(T));
                return true;
            }

            result = default;

            return false;
        } catch {
            result = default;

            return false;
        }
    }
}
