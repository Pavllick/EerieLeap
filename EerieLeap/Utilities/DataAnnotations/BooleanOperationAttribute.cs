using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace EerieLeap.Utilities.DataAnnotations;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public abstract class BooleanOperationAttribute : ValidationAttribute {
    protected BooleanOperationAttribute(BooleanOperation booleanOperation, [Required] object operandValue)
        : base("{0} must be {1} {2}.") {

        if ((int)booleanOperation <= (int)BooleanOperation.Null)
            throw new ArgumentException("Invalid boolean operation", nameof(booleanOperation));

        BooleanOperation = booleanOperation;
        OperandValue = operandValue;
    }

    public BooleanOperation BooleanOperation { get; }
    public object OperandValue { get; }

    public override bool IsValid(object? value) =>
        value == null || BooleanOperation.Operate(value, OperandValue);

    public override string FormatErrorMessage(string name) =>
        string.Format(CultureInfo.CurrentCulture, ErrorMessageString, name, BooleanOperation.GetOperationName(), OperandValue);
}
