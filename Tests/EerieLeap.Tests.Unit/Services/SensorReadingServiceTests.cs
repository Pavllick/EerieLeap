using EerieLeap.Configuration;
using EerieLeap.Hardware;
using EerieLeap.Services;
using EerieLeap.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Device.Spi;
using System.Text;
using System.Text.Json;
using Xunit;

namespace EerieLeap.Tests.Unit.Services;

public class SensorReadingServiceTests : IDisposable {
    private readonly string _testConfigPath;
    private readonly Mock<ILogger> _mockLogger;
    private readonly Mock<ILogger> _mockAdcFactoryLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IAdcConfigurationService> _mockAdcService;
    private readonly SensorReadingService _service;
    private readonly string _testDir;
    private readonly MockAdc _mockAdc;
    private bool _disposed;

    public SensorReadingServiceTests() {
        var tempDir = Path.GetTempPath();
        _testDir = Path.Combine(tempDir, "EerieLeapTests");
        if (Directory.Exists(_testDir)) {
            Directory.Delete(_testDir, true);
        }
        Directory.CreateDirectory(_testDir);
        _testConfigPath = _testDir;

        _mockLogger = new Mock<ILogger>();
        _mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        _mockAdcFactoryLogger = new Mock<ILogger>();
        _mockAdcFactoryLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        _mockConfiguration = new Mock<IConfiguration>();
        _mockAdcService = new Mock<IAdcConfigurationService>();

        var configSection = new Mock<IConfigurationSection>();
        configSection.Setup(x => x.Value).Returns(_testConfigPath);
        configSection.Setup(x => x.Path).Returns("ConfigurationPath");
        configSection.Setup(x => x["ConfigurationPath"]).Returns(_testConfigPath);
        _mockConfiguration.Setup(x => x.GetSection("ConfigurationPath")).Returns(configSection.Object);
        _mockConfiguration.Setup(x => x["ConfigurationPath"]).Returns(_testConfigPath);

        _mockAdc = new MockAdc();
        _mockAdcService.Setup(x => x.GetAdcAsync())
            .ReturnsAsync(_mockAdc);

        _service = new SensorReadingService(_mockLogger.Object, _mockAdcService.Object, _mockConfiguration.Object);
    }

    protected virtual void Dispose(bool disposing) {
        if (!_disposed) {
            if (disposing) {
                _service?.Dispose();
                _mockAdc?.Dispose();
            }
            if (Directory.Exists(_testDir)) {
                Directory.Delete(_testDir, true);
            }
            _disposed = true;
        }
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task StartAsync_WithValidConfig_InitializesCorrectly() {
        // Arrange
        var sensorConfig = new SensorConfig {
            Id = "temp_sensor_1",
            Name = "Temperature Sensor 1",
            Type = SensorType.Physical,
            Channel = 0,
            MinVoltage = 0,
            MaxVoltage = 5,
            MinValue = 0,
            MaxValue = 100,
            Unit = "C"
        };

        var adcConfig = new AdcConfig {
            Type = "MCP3008",
            BusId = 0,
            ChipSelect = 0,
            ClockFrequency = 1000000,
            Mode = SpiMode.Mode0,
            DataBitLength = 8,
            Resolution = 10, // 10-bit ADC
            ReferenceVoltage = 3.3, // Reference voltage in volts
            Protocol = new AdcProtocolConfig {
                CommandPrefix = Convert.FromHexString("40"),
                ChannelBitShift = 2,
                ChannelMask = 15,
                ResultBitMask = 1023, // 10-bit mask
                ResultBitShift = 0,
                ReadByteCount = 2
            }
        };

        var sensorsJsonPath = Path.Combine(_testConfigPath, "sensors.json");
        await File.WriteAllTextAsync(sensorsJsonPath, JsonSerializer.Serialize(new List<SensorConfig> { sensorConfig }));

        _mockAdc.Configure(adcConfig);

        // Act
        await _service.StartAsync(CancellationToken.None);
        await _service.WaitForInitializationAsync();

        // Assert
        // Service should start without throwing exceptions
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Never);
    }

    [Fact]
    public async Task StartAsync_WithInvalidConfig_ThrowsException() {
        // Arrange
        var invalidJson = "{ invalid json";
        var adcJsonPath = Path.Combine(_testConfigPath, "adc.json");
        var sensorsJsonPath = Path.Combine(_testConfigPath, "sensors.json");

        await File.WriteAllTextAsync(adcJsonPath, invalidJson, new UTF8Encoding(false));  // No BOM
        await File.WriteAllTextAsync(sensorsJsonPath, invalidJson, new UTF8Encoding(false));  // No BOM

        // Act & Assert
        await _service.StartAsync(CancellationToken.None);
        var ex = await Assert.ThrowsAsync<JsonException>(() =>
            _service.WaitForInitializationAsync());

        Assert.Contains("json value could not be converted", ex.Message.ToLowerInvariant());

        _mockLogger.Verify(
            x => x.Log(
            LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to load")),
                It.Is<Exception>(ex => ex is JsonException),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }
}
