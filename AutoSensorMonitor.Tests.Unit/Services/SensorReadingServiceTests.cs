using AutoSensorMonitor.Configuration;
using AutoSensorMonitor.Hardware;
using AutoSensorMonitor.Services;
using AutoSensorMonitor.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Device.Spi;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace AutoSensorMonitor.Tests.Unit.Services;

public class SensorReadingServiceTests : IDisposable
{
    private readonly string _testConfigPath;
    private readonly Mock<ILogger<SensorReadingService>> _mockLogger;
    private readonly Mock<ILogger<AdcFactory>> _mockAdcFactoryLogger;
    private readonly Mock<ILogger<Adc>> _mockAdcLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly SensorReadingService _service;
    private readonly string _testDir;

    public SensorReadingServiceTests()
    {
        var tempDir = Path.GetTempPath();
        _testDir = Path.Combine(tempDir, "AutoSensorMonitorTests");
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, true);
        }
        Directory.CreateDirectory(_testDir);
        _testConfigPath = _testDir;

        _mockLogger = new Mock<ILogger<SensorReadingService>>();
        _mockAdcFactoryLogger = new Mock<ILogger<AdcFactory>>();
        _mockAdcLogger = new Mock<ILogger<Adc>>();
        _mockConfiguration = new Mock<IConfiguration>();

        var configSection = new Mock<IConfigurationSection>();
        configSection.Setup(x => x.Value).Returns(_testConfigPath);
        _mockConfiguration.Setup(x => x.GetSection("ConfigurationPath")).Returns(configSection.Object);

        var adcFactory = new AdcFactory(_mockAdcFactoryLogger.Object, _mockAdcLogger.Object);
        _service = new SensorReadingService(_mockLogger.Object, adcFactory, _mockConfiguration.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, true);
        }
    }

    [Fact]
    public async Task StartAsync_WithValidConfig_InitializesCorrectly()
    {
        // Arrange
        var sensorConfig = new SensorConfig
        {
            Id = "temp_sensor_1",
            Name = "Temperature Sensor 1",
            Type = SensorType.Temperature,
            Channel = 0,
            MinVoltage = 0,
            MaxVoltage = 5,
            MinValue = 0,
            MaxValue = 100,
            Unit = "C"
        };

        var adcConfig = new AdcConfig
        {
            Type = "MCP3008",
            BusId = 0,
            ChipSelect = 0,
            ClockFrequency = 1000000,
            Mode = SpiMode.Mode0,
            DataBitLength = 8
        };

        var combinedConfig = new CombinedConfig
        {
            SensorConfigs = new List<SensorConfig> { sensorConfig },
            AdcConfig = adcConfig
        };

        var adcJsonPath = Path.Combine(_testConfigPath, "adc.json");
        var sensorsJsonPath = Path.Combine(_testConfigPath, "sensors.json");

        await File.WriteAllTextAsync(adcJsonPath, JsonSerializer.Serialize(adcConfig));
        await File.WriteAllTextAsync(sensorsJsonPath, JsonSerializer.Serialize(new List<SensorConfig> { sensorConfig }));

        // Act
        await _service.StartAsync(CancellationToken.None);

        // Assert
        // Service should start without throwing exceptions
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Never);
    }

    [Fact]
    public async Task StartAsync_WithInvalidConfig_ThrowsException()
    {
        // Arrange
        var invalidJson = "{ invalid json";
        var adcJsonPath = Path.Combine(_testConfigPath, "adc.json");
        var sensorsJsonPath = Path.Combine(_testConfigPath, "sensors.json");

        await File.WriteAllTextAsync(adcJsonPath, invalidJson);
        await File.WriteAllTextAsync(sensorsJsonPath, invalidJson);

        // Act & Assert
        await _service.StartAsync(CancellationToken.None);

        var ex = await Assert.ThrowsAsync<JsonException>(() => _service.WaitForInitializationAsync());
        Assert.Contains("invalid start of a property name", ex.Message);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to load configurations")),
                It.Is<Exception>(ex => ex is JsonException),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
