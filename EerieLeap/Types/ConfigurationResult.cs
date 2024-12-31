namespace EerieLeap.Types;

public record ConfigurationError(string? Id, string Message);

public class ConfigurationResult {
    public bool Success { get; }
    public IReadOnlyList<ConfigurationError> Errors { get; }

    public ConfigurationResult(bool success, IEnumerable<ConfigurationError>? errors = null) {
        Success = success;
        Errors = errors?.ToList().AsReadOnly() ?? Array.Empty<ConfigurationError>().AsReadOnly();
    }

    public ConfigurationResult(bool success, IEnumerable<string> errors) {
        Success = success;
        Errors = errors.Select(e => new ConfigurationError(null, e)).ToList().AsReadOnly();
    }

    public static ConfigurationResult CreateSuccess() =>
        new(true);

    public static ConfigurationResult CreateFailure(IEnumerable<ConfigurationError> errors) =>
        new(false, errors.ToList());

    public static ConfigurationResult CreateFailure(IEnumerable<string> errors) =>
        new(false, errors.ToList());
}

public class ConfigurationResult<T> : ConfigurationResult {
    public T? Data { get; }

    public ConfigurationResult(bool success, IEnumerable<ConfigurationError>? errors = null) : base(success, errors) { }

    public ConfigurationResult(bool success, IEnumerable<string> errors) : base(success, errors) { }

    public ConfigurationResult(bool success, T data) : base(success) =>
        Data = data;
}
