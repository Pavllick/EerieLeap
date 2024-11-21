using System.ComponentModel.DataAnnotations;
using EerieLeap.Utilities.DataAnnotations;
using Xunit;

namespace EerieLeap.Tests.Unit.DataAnnotations;

public class BooleanOperationAttributeTests {
    private class TestBooleanOperationAttribute : BooleanOperationAttribute {
        public TestBooleanOperationAttribute(BooleanOperation operation, object operandValue)
            : base(operation, operandValue) { }
    }

    private class TestModel {
        [GreaterThan(10)]
        public int Value { get; set; }
    }

    [Theory]
    [InlineData(BooleanOperation.GreaterThan, 10, 20, true)]
    [InlineData(BooleanOperation.GreaterThan, 10, 5, false)]
    [InlineData(BooleanOperation.LessThan, 10, 5, true)]
    [InlineData(BooleanOperation.LessThan, 10, 20, false)]
    [InlineData(BooleanOperation.GreaterThanOrEqualTo, 10, 10, true)]
    [InlineData(BooleanOperation.GreaterThanOrEqualTo, 10, 5, false)]
    [InlineData(BooleanOperation.LessThanOrEqualTo, 10, 10, true)]
    [InlineData(BooleanOperation.LessThanOrEqualTo, 10, 20, false)]
    public void IsValid_WithVariousOperations_ReturnsExpectedResult(
        BooleanOperation operation,
        int operandValue,
        int testValue,
        bool expectedResult) {
        var attribute = new TestBooleanOperationAttribute(operation, operandValue);
        var result = attribute.IsValid(testValue);
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void IsValid_WithNullValue_ReturnsTrue() {
        var attribute = new TestBooleanOperationAttribute(BooleanOperation.GreaterThan, 10);
        var result = attribute.IsValid(null);
        Assert.True(result);
    }

    [Fact]
    public void Constructor_WithNullOperandValue_ThrowsArgumentNullException() {
        Assert.Throws<ArgumentNullException>(() =>
            new TestBooleanOperationAttribute(BooleanOperation.GreaterThan, null!));
    }

    [Fact]
    public void Constructor_WithNullOperation_ThrowsArgumentException() {
        Assert.Throws<ArgumentException>(() =>
            new TestBooleanOperationAttribute(BooleanOperation.Null, 10));
    }

    [Fact]
    public void FormatErrorMessage_ReturnsFormattedMessage() {
        var attribute = new GreaterThanAttribute(10);
        var message = attribute.FormatErrorMessage("TestProperty");
        Assert.Equal("TestProperty must be greater than 10.", message);
    }

    [Fact]
    public void ValidationContext_WithInvalidValue_ReturnsValidationResult() {
        var model = new TestModel { Value = 5 };
        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(model, context, results, true);

        Assert.False(isValid);
        Assert.Single(results);
        Assert.Contains("Value must be greater than 10.", results[0].ErrorMessage);
    }
}
