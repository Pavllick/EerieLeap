using System.Text.Json;
using System.Text.Json.Serialization;
using EerieLeap.Utilities.Converters;
using Xunit;

namespace EerieLeap.Tests.Unit.Utilities.Converters;

public class HexByteArrayConverterTests {
    private readonly JsonSerializerOptions _options;

    public HexByteArrayConverterTests() {
        _options = new JsonSerializerOptions();
    }

    private class TestClass {
        [JsonConverter(typeof(HexByteArrayJsonConverter))]
        public byte[]? Data { get; set; }
    }

    [Theory]
    [InlineData("null", new byte[0])]
    [InlineData("[]", new byte[0])]
    [InlineData("[\"0x01\", \"0x02\"]", new byte[] { 0x01, 0x02 })]
    public void Deserialize_ValidHexArray_ReturnsExpectedBytes(string jsonData, byte[] expected) {
        var json = $"{{\"Data\": {jsonData}}}";

        var testObject = JsonSerializer.Deserialize<TestClass>(json, _options);

        Assert.NotNull(testObject);
        Assert.Equal(expected, testObject?.Data);
    }

    [Theory]
    [InlineData(new byte[] { 0x01, 0x02 }, new[] { "0x01", "0x02" })]
    [InlineData(new byte[0], new string[0])]
    [InlineData(new byte[] { 0x0f }, new[] { "0x0F" })]
    public void Serialize_ByteArray_ReturnsExpectedHexArray(byte[] input, string[] expected) {
        var testObject = new TestClass { Data = input };
        var json = JsonSerializer.Serialize(testObject, _options);

        var expectedJson = $"{{\"Data\":[{string.Join(",", expected.Select(h => $"\"{h}\""))}]}}";
        Assert.Equal(expectedJson, json);
    }

    [Theory]
    [InlineData("0x0g")]
    [InlineData("invalid")]
    [InlineData("0xinvalid")]
    public void Deserialize_InvalidHexString_ThrowsJsonException(string hexString) {
        var json = $"{{\"Data\": \"{hexString}\"}}";
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<TestClass>(json, _options));
    }

    [Fact]
    public void Deserialize_NonStringValue_ThrowsJsonException() {
        var json = "{\"Data\": 123}";
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<TestClass>(json, _options));
    }
}
