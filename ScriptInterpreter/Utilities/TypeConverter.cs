namespace ScriptInterpreter.Utilities;

public static class TypeConverter {
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
