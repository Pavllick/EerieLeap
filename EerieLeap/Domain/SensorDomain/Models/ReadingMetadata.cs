namespace EerieLeap.Domain.SensorDomain.Models;

public record ReadingMetadata {
    public Dictionary<string, string> Tags { get; }

    public ReadingMetadata(Dictionary<string, string>? tags = null) =>
        Tags = tags ?? new Dictionary<string, string>();

    public void AddTag(string key, string value) =>
        Tags.Add(key, value);
}
