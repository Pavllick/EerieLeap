using NCalc;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace EerieLeap.Utilities;

public static class ExpressionEvaluator {
    private static readonly Regex _sensorIdRegex = new(@"\{([a-z_][a-z0-9_]*)\}", RegexOptions.Compiled);

    private static void AddMathConstants(Expression expr) {
        expr.Parameters["PI"] = Math.PI;
        expr.Parameters["E"] = Math.E;
    }

    public static double Evaluate(string expression, double x, Dictionary<string, double>? sensorValues = null) {
        if (string.IsNullOrWhiteSpace(expression))
            return x;

        return Evaluate(expression, new Dictionary<string, double>(sensorValues ?? new Dictionary<string, double>()) {
            { "x", x }
        });
    }

    public static double Evaluate([Required] string expression, [Required] Dictionary<string, double> sensorValues) {
        //ArgumentNullException.ThrowIfNull(sensorValues);

        var expr = new Expression(UnwrapVariables(expression));
        AddMathConstants(expr);

        foreach (var (sensorId, value) in sensorValues)
            expr.Parameters[sensorId] = value;

        var result = expr.Evaluate();
        if (result is double d)
            return d;

        throw new InvalidOperationException($"Expression evaluation did not return a number: {result}");
    }

    public static HashSet<string> ExtractSensorIds(string expression) {
        var matches = _sensorIdRegex.Matches(expression);
        var sensorIds = new HashSet<string>();

        // Group[1] contains the sensor ID without curly braces
        foreach (Match match in matches)
            sensorIds.Add(match.Groups[1].Value);

        return sensorIds;
    }

    private static string UnwrapVariables(string expression) =>
        _sensorIdRegex.Replace(expression, "${1}");
}
