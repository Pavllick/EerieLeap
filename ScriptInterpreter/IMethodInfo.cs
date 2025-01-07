namespace ScriptInterpreter;

public interface IMethodInfo {
    public string Name { get; set; }
    public bool IsOptional { get; set; }
    public bool IsAvailable { get; set; }
    public ParameterInfo[] Parameters { get; set; }
    void SetExcecuteHandler(Func<object[], object> handler);
    void Reset();
}
