namespace ValidationProcessor.DataAnnotations;

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class IgnoreCustomValidationAttribute : Attribute { }
