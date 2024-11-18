namespace EerieLeap.Utilities.DataAnnotations;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class GreaterThanAttribute : BooleanOperationAttribute
{
    public GreaterThanAttribute(object operandValue)
        : base(BooleanOperation.GreaterThan, operandValue)
    { }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class LessThanAttribute : BooleanOperationAttribute
{
    public LessThanAttribute(object operandValue)
        : base(BooleanOperation.LessThan, operandValue)
    { }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class GreaterThanOrEqualToAttribute : BooleanOperationAttribute
{
    public GreaterThanOrEqualToAttribute(object operandValue)
        : base(BooleanOperation.GreaterThanOrEqualTo, operandValue)
    { }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class LessThanOrEqualToAttribute : BooleanOperationAttribute
{
    public LessThanOrEqualToAttribute(object operandValue)
        : base(BooleanOperation.LessThanOrEqualTo, operandValue)
    { }
}
