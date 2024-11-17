using NCalc;
using System.Text.RegularExpressions;

namespace EerieLeap.Services;

public static class ExpressionEvaluator
{
    private static readonly Regex SensorIdRegex = new(@"\{([a-z_][a-z0-9_]*)\}", RegexOptions.Compiled);

    private static void AddMathConstants(Expression expr)
    {
        expr.Parameters["PI"] = Math.PI;
        expr.Parameters["E"] = Math.E;
    }

    public static double Evaluate(string expression, double x)
    {
        var expr = new Expression(UnwrapVariables(expression));
        expr.Parameters["x"] = x;
        AddMathConstants(expr);
        
        var result = expr.Evaluate();
        if (result is double d)
        {
            return d;
        }
        
        throw new InvalidOperationException($"Expression evaluation did not return a number: {result}");
    }

    public static double EvaluateWithSensors(string expression, Dictionary<string, double> sensorValues)
    {
        var expr = new Expression(UnwrapVariables(expression));
        AddMathConstants(expr);
        
        foreach (var (sensorId, value) in sensorValues)
        {
            expr.Parameters[sensorId] = value;
        }
        
        var result = expr.Evaluate();
        if (result is double d)
        {
            return d;
        }
        
        throw new InvalidOperationException($"Expression evaluation did not return a number: {result}");
    }

    public static HashSet<string> ExtractSensorIds(string expression)
    {
        var matches = SensorIdRegex.Matches(expression);
        var sensorIds = new HashSet<string>();
        
        foreach (Match match in matches)
        {
            // Group[1] contains the sensor ID without curly braces
            sensorIds.Add(match.Groups[1].Value);
        }

        return sensorIds;
    }

    private static string UnwrapVariables(string expression)
    {
        // Replace {varName} with varName for NCalc evaluation
        return SensorIdRegex.Replace(expression, "${1}");
    }
}
