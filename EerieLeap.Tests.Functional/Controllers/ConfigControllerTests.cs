using EerieLeap.Configuration;
using EerieLeap.Tests.Functional.Infrastructure;
using EerieLeap.Types;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Device.Spi;
using System.Net;
using Xunit;

namespace EerieLeap.Tests.Functional.Controllers;

public class ConfigControllerTests : FunctionalTestBase {
    public ConfigControllerTests(WebApplicationFactory<Program> factory)
        : base(factory) { }

    [Fact]
    public async Task GetConfig_ReturnsSuccessStatusCode() {
        // Act
        var config = await GetAsync<CombinedConfig>("api/v1/config");

        // Assert
        Assert.NotNull(config);
        Assert.NotNull(config.AdcConfig);
        Assert.NotNull(config.SensorConfigs);
    }

    [Fact]
    public async Task GetAdcConfig_ReturnsSuccessStatusCode() {
        // Act
        var config = await GetAsync<AdcConfig>("api/v1/config/adc");

        // Assert
        Assert.NotNull(config);
    }

    [Fact]
    public async Task GetSensorConfigs_ReturnsSuccessStatusCode() {
        // Act
        var configs = await GetAsync<List<SensorConfig>>("api/v1/config/sensors");

        // Assert
        Assert.NotNull(configs);
    }

    [Fact]
    public async Task UpdateAdcConfig_WithValidAdcConfig_ReturnsSuccessStatusCode() {
        // Arrange
        var config = new AdcConfig {
            Type = "MCP3008",
            Resolution = 10,
            ReferenceVoltage = 3.3,
            ClockFrequency = 1_000_000,
            BusId = 0,
            ChipSelect = 0,
            Mode = SpiMode.Mode0,
            DataBitLength = 8,
            Protocol = new AdcProtocolConfig()
        };

        // Act
        var response = await PostAsync("api/v1/config/adc", config);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify all properties were updated correctly
        var updatedConfig = await GetAsync<AdcConfig>("api/v1/config/adc");
        Assert.Equal(config.Type, updatedConfig!.Type);
        Assert.Equal(config.Resolution, updatedConfig.Resolution);
        Assert.Equal(config.ReferenceVoltage, updatedConfig.ReferenceVoltage);
        Assert.Equal(config.ClockFrequency, updatedConfig.ClockFrequency);
        Assert.Equal(config.BusId, updatedConfig.BusId);
        Assert.Equal(config.ChipSelect, updatedConfig.ChipSelect);
        Assert.Equal(config.Mode, updatedConfig.Mode);
        Assert.Equal(config.DataBitLength, updatedConfig.DataBitLength);
        Assert.NotNull(updatedConfig.Protocol);
    }

    [Fact]
    public async Task UpdateAdcConfig_WithInvalidAdcConfig_ReturnsBadRequest() {
        // Arrange
        var config = new AdcConfig(); // Missing required fields

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(async () => {
            var response = await PostAsync("api/v1/config/adc", config);
            response.EnsureSuccessStatusCode();
        });

        Assert.Contains("400", exception.Message);
    }

    [Fact]
    public async Task UpdateSensorConfigs_WithValidConfigs_ReturnsSuccessStatusCode() {
        // Arrange
        var configs = new List<SensorConfig>
        {
            new SensorConfig
            {
                Id = "test_sensor_1",
                Name = "Test Sensor 1",
                Type = SensorType.Temperature,
                Unit = "°C",
                Channel = 0,
                MinVoltage = 0,
                MaxVoltage = 3.3,
                MinValue = -40,
                MaxValue = 125
            },
            new SensorConfig
            {
                Id = "test_sensor_2",
                Name = "Test Sensor 2",
                Type = SensorType.Pressure,
                Unit = "kPa",
                Channel = 1,
                MinVoltage = 0,
                MaxVoltage = 3.3,
                MinValue = 0,
                MaxValue = 100
            }
        };

        // Act
        var response = await PostAsync("api/v1/config/sensors", configs);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify the update
        var updatedConfigs = await GetAsync<List<SensorConfig>>("api/v1/config/sensors");
        Assert.Contains(updatedConfigs!, c => c.Id == "test_sensor_1");
        Assert.Contains(updatedConfigs!, c => c.Id == "test_sensor_2");
    }

    [Fact]
    public async Task UpdateSensorConfigs_WithValidPhysicalSensor_ValidatesAllProperties() {
        // Arrange
        var config1 = new SensorConfig {
            Id = "physical_sensor_1",
            Name = "Physical Temperature Sensor",
            Type = SensorType.Temperature,
            Unit = "°C",
            Channel = 0,
            MinVoltage = 0,
            MaxVoltage = 3.3,
            MinValue = -40,
            MaxValue = 125,
            SamplingRateMs = 1000
        };

        var config2 = new SensorConfig {
            Id = "physical_sensor_2",
            Name = "Physical Temperature Sensor",
            Type = SensorType.Temperature,
            Unit = "°C",
            Channel = 1,
            MinVoltage = 0,
            MaxVoltage = 3.3,
            MinValue = -40,
            MaxValue = 125,
            SamplingRateMs = 1000,
            ConversionExpression = "x + 10"
        };

        await PostSensorConfigsWithDelay(new List<SensorConfig> { config1, config2 });

        // Act
        var configs = await GetAsync<IEnumerable<SensorConfig>>("api/v1/config/sensors");

        // Validate sensor config was stored correctly
        var storedConfig1 = configs!.First(c => c.Id == config1.Id);
        Assert.Equal(config1.Name, storedConfig1.Name);
        Assert.Equal(config1.Type, storedConfig1.Type);
        Assert.Equal(config1.Unit, storedConfig1.Unit);
        Assert.Equal(config1.Channel, storedConfig1.Channel);
        Assert.Equal(config1.MinVoltage, storedConfig1.MinVoltage);
        Assert.Equal(config1.MaxVoltage, storedConfig1.MaxVoltage);
        Assert.Equal(config1.MinValue, storedConfig1.MinValue);
        Assert.Equal(config1.MaxValue, storedConfig1.MaxValue);
        Assert.Equal(config1.SamplingRateMs, storedConfig1.SamplingRateMs);
        Assert.Null(storedConfig1.ConversionExpression);

        var storedConfig2 = configs!.First(c => c.Id == config2.Id);
        Assert.Equal(config2.Name, storedConfig2.Name);
        Assert.Equal(config2.Type, storedConfig2.Type);
        Assert.Equal(config2.Unit, storedConfig2.Unit);
        Assert.Equal(config2.Channel, storedConfig2.Channel);
        Assert.Equal(config2.MinVoltage, storedConfig2.MinVoltage);
        Assert.Equal(config2.MaxVoltage, storedConfig2.MaxVoltage);
        Assert.Equal(config2.MinValue, storedConfig2.MinValue);
        Assert.Equal(config2.MaxValue, storedConfig2.MaxValue);
        Assert.Equal(config2.SamplingRateMs, storedConfig2.SamplingRateMs);
        Assert.Equal(config2.ConversionExpression, storedConfig2.ConversionExpression);
    }

    [Fact]
    public async Task UpdateSensorConfigs_WithValidVirtualSensor_ValidatesAllProperties() {
        // Arrange
        var physicalConfig = new SensorConfig {
            Id = "physical_temp_1",
            Name = "Physical Temperature 1",
            Type = SensorType.Temperature,
            Unit = "°C",
            Channel = 0,
            MinVoltage = 0,
            MaxVoltage = 3.3,
            MinValue = -40,
            MaxValue = 125,
            SamplingRateMs = 1000
        };

        var virtualConfig = new SensorConfig {
            Id = "virtual_avg_temp",
            Name = "Virtual Average Temperature",
            Type = SensorType.Virtual,
            Unit = "°C",
            MinValue = -40,
            MaxValue = 125,
            SamplingRateMs = 500,
            ConversionExpression = "{physical_temp_1} * 0.8" // Simple scaling of the physical sensor
        };

        await PostSensorConfigsWithDelay(new List<SensorConfig> { physicalConfig, virtualConfig });

        // Get the stored configs
        var configs = await GetAsync<IEnumerable<SensorConfig>>("api/v1/config/sensors");

        // Validate virtual sensor config was stored correctly
        var storedConfig = configs!.First(c => c.Id == virtualConfig.Id);
        Assert.Equal(virtualConfig.Name, storedConfig.Name);
        Assert.Equal(virtualConfig.Type, storedConfig.Type);
        Assert.Equal(virtualConfig.Unit, storedConfig.Unit);
        Assert.Equal(virtualConfig.MinValue, storedConfig.MinValue);
        Assert.Equal(virtualConfig.MaxValue, storedConfig.MaxValue);
        Assert.Equal(virtualConfig.SamplingRateMs, storedConfig.SamplingRateMs);
        Assert.Equal(virtualConfig.ConversionExpression, storedConfig.ConversionExpression);

        // Virtual sensors should not have physical sensor properties
        Assert.Null(storedConfig.Channel);
        Assert.Null(storedConfig.MinVoltage);
        Assert.Null(storedConfig.MaxVoltage);
    }

    [Fact]
    public async Task UpdateSensorConfigs_WithInvalidConfig_ReturnsBadRequest() {
        // Arrange
        var configs = new List<SensorConfig>
        {
            new SensorConfig
            {
                // Missing required fields
                Id = "test_sensor_1",
                Type = SensorType.Temperature
            }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(async () => {
            var response = await PostAsync("api/v1/config/sensors", configs);
            response.EnsureSuccessStatusCode();
        });

        Assert.Contains("400", exception.Message);
    }

    [Fact]
    public async Task UpdateSensorConfigs_WithDuplicateIds_ReturnsBadRequest() {
        // Arrange
        var configs = new List<SensorConfig>
        {
            new SensorConfig
            {
                Id = "test_sensor_1",
                Name = "Test Sensor 1",
                Type = SensorType.Temperature,
                Unit = "°C",
                Channel = 0,
                MinVoltage = 0,
                MaxVoltage = 3.3,
                MinValue = -40,
                MaxValue = 125,
                SamplingRateMs = 1000
            },
            new SensorConfig
            {
                Id = "test_sensor_1", // Same ID as above
                Name = "Test Sensor 2",
                Type = SensorType.Temperature,
                Unit = "°C",
                Channel = 1,
                MinVoltage = 0,
                MaxVoltage = 3.3,
                MinValue = -40,
                MaxValue = 125,
                SamplingRateMs = 1000
            }
        };

        // Act
        var response = await PostWithFullResponse("api/v1/config/sensors", configs);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetSensorConfig_WithValidId_ReturnsConfig() {
        // Arrange
        var config = new SensorConfig {
            Id = "test_sensor_1",
            Name = "Test Sensor 1",
            Type = SensorType.Temperature,
            Unit = "°C",
            Channel = 0,
            MinVoltage = 0,
            MaxVoltage = 3.3,
            MinValue = -40,
            MaxValue = 125,
            SamplingRateMs = 1000
        };

        await PostSensorConfigsWithDelay(new List<SensorConfig> { config });

        // Act
        var response = await GetAsync<SensorConfig>($"api/v1/config/sensors/{config.Id}");

        // Assert
        Assert.NotNull(response);
        Assert.Equal(config.Id, response.Id);
        Assert.Equal(config.Type, response.Type);
        Assert.Equal(config.Channel, response.Channel);
        Assert.Equal(config.SamplingRateMs, response.SamplingRateMs);
    }

    [Fact]
    public async Task GetSensorConfig_WithInvalidId_ReturnsBadRequest() {
        // Act
        var response = await Client.GetAsync("api/v1/config/sensors/invalid-id");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetSensorConfig_WithInvalidIdFormat_ReturnsBadRequest() {
        // Act
        var response = await GetWithFullResponse("api/v1/config/sensors/invalid!id");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
