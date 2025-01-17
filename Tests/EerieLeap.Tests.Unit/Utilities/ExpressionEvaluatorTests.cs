using System.ComponentModel.DataAnnotations;
using EerieLeap.Utilities;
using NCalc;
using Xunit;

namespace EerieLeap.Tests.Unit.Utilities;

public class ExpressionEvaluatorTests {
    [Theory]
    [InlineData("2 * x + 1", 3, 7)]
    [InlineData("x * x", 4, 16)]
    [InlineData("PI * x * x", 2, Math.PI * 4)]
    [InlineData("E * x", 1, Math.E)]
    public void Evaluate_WithValidExpression_ReturnsCorrectResult(string expression, double x, double expected) {
        // Act
        var result = ExpressionEvaluator.Evaluate(expression, x);

        // Assert
        Assert.Equal(expected, result, 6); // 6 decimal places precision
    }

    [Theory]
    [InlineData("2 * {sensor1} + {sensor2}", new[] { "sensor1", "sensor2" }, new[] { 3.0, 1.0 }, 7.0)]
    [InlineData("{temp1} * {temp2}", new[] { "temp1", "temp2" }, new[] { 4.0, 4.0 }, 16.0)]
    public void EvaluateWithSensors_WithValidExpression_ReturnsCorrectResult(
        string expression, string[] sensorIds, double[] values, double expected) {
        // Arrange
        var sensorValues = sensorIds.Zip(values, (id, value) => (id, value))
                                  .ToDictionary(x => x.id, x => x.value);

        // Act
        var result = ExpressionEvaluator.Evaluate(expression, sensorValues);

        // Assert
        Assert.Equal(expected, result, 6);
    }

    [Theory]
    [InlineData("2 * {sensor1} + {sensor2}", new[] { "sensor1", "sensor2" })]
    [InlineData("{temp1} + {temp2} + {temp3}", new[] { "temp1", "temp2", "temp3" })]
    public void ExtractSensorIds_ReturnsCorrectIds(string expression, string[] expectedIds) {
        // Act
        var sensorIds = ExpressionEvaluator.ExtractSensorIds(expression);

        // Assert
        Assert.Equal(expectedIds.ToHashSet(), sensorIds);
    }

    [Theory]
    [InlineData("2 * x + invalid")]
    public void Evaluate_WithInvalidExpression_ThrowsArgumentException(string expression) {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => ExpressionEvaluator.Evaluate(expression, 1));
    }

    [Theory]
    [InlineData("invalid expression")]
    public void Evaluate_WithInvalidExpression_ThrowsEvaluationException(string expression) {
        // Act & Assert
        Assert.Throws<EvaluationException>(() => ExpressionEvaluator.Evaluate(expression, 1));
    }

    [Theory]
    [InlineData("2 * {sensor1} + invalid", new[] { "sensor1" }, new[] { 1.0 })]
    public void EvaluateWithSensors_WithInvalidExpression_ThrowsArgumentException(
        string expression, string[] sensorIds, double[] values) {
        // Arrange
        var sensorValues = sensorIds.Zip(values, (id, value) => (id, value))
                                  .ToDictionary(x => x.id, x => x.value);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => ExpressionEvaluator.Evaluate(expression, sensorValues));
    }

    [Fact]
    public void EvaluateWithSensors_WithNullSensorValues_ThrowsArgumentNullException() {
        // Arrange
        const string expression = "1 + 2";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ExpressionEvaluator.Evaluate(expression, null!));
    }

    // TODO: Uncomment when validations are implemented
    [Theory]
    [InlineData(null, typeof(ArgumentNullException))]
    // [InlineData("", typeof(ValidationException))]
    // [InlineData(" ")]
    // [InlineData("\t")]
    public void EvaluateWithSensors_WithEmptyExpression_ThrowsValidationException(string expression, Type exceptionType) {
        // Arrange
        var sensorValues = new Dictionary<string, double> { { "sensor1", 1.0 } };

        // Act & Assert
        var ex = Assert.ThrowsAny<Exception>(() => ExpressionEvaluator.Evaluate(expression, sensorValues));
        Assert.IsType(exceptionType, ex);
        Assert.Contains("expression", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
