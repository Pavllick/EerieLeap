namespace ScriptInterpreter;

public class ParameterInfo {
    public string Name { get; set; }
    public Type Type { get; set; } = typeof(object);
    public bool IsOptional { get; set; } = false;
}
