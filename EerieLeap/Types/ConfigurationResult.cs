namespace EerieLeap.Types;

public record ConfigurationError(string SensorId, string Message);

public class ConfigurationResult {
    public bool Success { get; }
    public IReadOnlyList<ConfigurationError> Errors { get; }

    private ConfigurationResult(bool success, IReadOnlyList<ConfigurationError> errors) {
        Success = success;
        Errors = errors;
    }

    public static ConfigurationResult CreateSuccess() => 
        new(true, Array.Empty<ConfigurationError>());

    public static ConfigurationResult CreateFailure(IEnumerable<ConfigurationError> errors) => 
        new(false, errors.ToList());
}
