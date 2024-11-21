using System.Text.Json;
using System.Text.Json.Serialization;
using EerieLeap.Utilities;
using Xunit;

namespace EerieLeap.Tests.Unit.Utilities;

public class HexByteArrayConverterTests {
    private readonly JsonSerializerOptions _options;

    public HexByteArrayConverterTests() {
        _options = new JsonSerializerOptions();
    }

    private class TestClass {
        [JsonConverter(typeof(HexByteArrayConverter))]
        public byte[]? Data { get; set; }
    }

    [Theory]
    [InlineData("0x0102", new byte[] { 0x01, 0x02 })]
    [InlineData("0102", new byte[] { 0x01, 0x02 })]
    [InlineData("", new byte[0])]
    [InlineData(null, new byte[0])]
    [InlineData("0xf", new byte[] { 0x0f })]
    [InlineData("f", new byte[] { 0x0f })]
    public void Deserialize_ValidHexString_ReturnsExpectedBytes(string? hexString, byte[] expected) {
        var json = hexString == null ? "null" : $"\"{hexString}\"";
        var testObject = JsonSerializer.Deserialize<TestClass>($"{{\"Data\": {json}}}", _options);

        Assert.NotNull(testObject);
        Assert.Equal(expected, testObject.Data);
    }

    [Theory]
    [InlineData(new byte[] { 0x01, 0x02 }, "0x0102")]
    [InlineData(new byte[0], "0x")]
    [InlineData(new byte[] { 0x0f }, "0x0F")]
    public void Serialize_ByteArray_ReturnsExpectedHexString(byte[] input, string expected) {
        var testObject = new TestClass { Data = input };
        var json = JsonSerializer.Serialize(testObject, _options);

        var expectedJson = $"{{\"Data\":\"{expected}\"}}";
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
