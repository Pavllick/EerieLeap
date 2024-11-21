using System.Globalization;

namespace EerieLeap.Utilities.DataAnnotations;

public enum BooleanOperation {
    Null = 0,
    EqualTo,
    NotEqualTo,
    GreaterThan,
    LessThan,
    GreaterThanOrEqualTo,
    LessThanOrEqualTo
}

public static class BooleanOperationExtensions {
    public static bool Operate(this BooleanOperation operation, object value1, object value2) {
        if (value1 == null || value2 == null)
            return false;

        if (!value1.GetType().IsValueType || !value2.GetType().IsValueType)
            return false;

        var comparison = ((IComparable)value1).CompareTo(value2);

        return operation switch {
            BooleanOperation.EqualTo => comparison == 0,
            BooleanOperation.NotEqualTo => comparison != 0,
            BooleanOperation.GreaterThan => comparison > 0,
            BooleanOperation.LessThan => comparison < 0,
            BooleanOperation.GreaterThanOrEqualTo => comparison >= 0,
            BooleanOperation.LessThanOrEqualTo => comparison <= 0,
            _ => false
        };
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Used by UI")]
    public static string GetOperationName(this BooleanOperation operation) =>
        operation.ToString().SpaceCamelCase().ToLowerInvariant();
}
