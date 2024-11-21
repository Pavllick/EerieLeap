using EerieLeap.Utilities;
using Xunit;

namespace EerieLeap.Tests.Unit.Utilities;

public class StringExtensionsTests {
    [Theory]
    [InlineData("camelCase", "camel case")]
    [InlineData("PascalCase", "pascal case")]
    [InlineData("ABC", "a b c")]
    [InlineData("simpletext", "simpletext")]
    [InlineData("", "")]
    [InlineData(null, null)]
    [InlineData("XMLHttpRequest", "x m l http request")]
    [InlineData("iPhone", "i phone")]
    [InlineData("CDC", "c d c")]
    [InlineData("SensorType", "sensor type")]
    public void SpaceCamelCase_WithVariousInputs_ReturnsExpectedResult(string input, string expected) {
        var result = input.SpaceCamelCase();
        Assert.Equal(expected, result);
    }
}
