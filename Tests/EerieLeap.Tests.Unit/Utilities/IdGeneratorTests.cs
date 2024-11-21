using EerieLeap.Utilities;
using Xunit;

namespace EerieLeap.Tests.Unit.Utilities;

public class IdGeneratorTests {
    [Theory]
    [InlineData("Test Name", "test_name")]
    [InlineData("test name", "test_name")]
    [InlineData("Test@Name", "testname")]
    [InlineData("Test123", "test123")]
    [InlineData("123Test", "123test")]
    [InlineData("Test_Name", "test_name")]
    [InlineData("TEST NAME", "test_name")]
    [InlineData("!@#$%^", "")] // Should generate GUID-based ID
    public void GenerateId_WithValidInput_ReturnsExpectedId(string input, string expectedPrefix) {
        var result = IdGenerator.GenerateId(input);

        if (string.IsNullOrEmpty(expectedPrefix))
            Assert.Matches("^[a-f0-9]{6}$", result);
        else
            Assert.Equal(expectedPrefix, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void GenerateId_WithNullOrEmptyInput_ReturnsGuidBasedId(string? input) {
        var result = IdGenerator.GenerateId(input);

        Assert.Matches("^[a-f0-9]{6}$", result);
    }

    [Fact]
    public void GenerateId_WithNoInput_ReturnsUniqueIds() {
        var results = new HashSet<string>();
        for (int i = 0; i < 1000; i++) {
            var id = IdGenerator.GenerateId();
            Assert.True(results.Add(id), "Generated ID was not unique");
            Assert.Matches("^[a-f0-9]{6}$", id);
        }
    }

    [Theory]
    [InlineData("a b c", "a_b_c")]
    [InlineData("A B C", "a_b_c")]
    [InlineData("a  b  c", "a_b_c")]
    [InlineData(" a b c ", "a_b_c")]
    public void GenerateId_WithMultipleSpaces_HandlesThemCorrectly(string input, string expected) {
        var result = IdGenerator.GenerateId(input);
        Assert.Equal(expected, result);
    }
}
