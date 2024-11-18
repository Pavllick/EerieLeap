using EerieLeap.Configuration;
using EerieLeap.Tests.Functional.Infrastructure;
using EerieLeap.Types;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Xunit;

namespace EerieLeap.Tests.Functional.Controllers;

public class ReadingsControllerTests : FunctionalTestBase
{
    public ReadingsControllerTests(WebApplicationFactory<Program> factory) 
        : base(factory)
    { }

    [Fact]
    public async Task GetReadings_ReturnsSuccessStatusCode()
    {
        // Act
        var readings = await GetAsync<IEnumerable<ReadingResult>>("api/v1/readings");

        // Assert
        Assert.NotNull(readings);
    }

    [Fact]
    public async Task GetReadings_WithPhysicalSensor_ReturnsValidReadings()
    {
        // Arrange
        var config1 = new SensorConfig
        {
            Id = "physical_sensor_1",
            Name = "Physical Temperature Sensor 1",
            Type = SensorType.Temperature,
            Unit = "°C",
            Channel = 0,
            MinVoltage = 0,
            MaxVoltage = 3.3,
            MinValue = -40,
            MaxValue = 125,
            SamplingRateMs = 1000
        };

        var config2 = new SensorConfig
        {
            Id = "physical_sensor_2",
            Name = "Physical Temperature Sensor 2",
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
        var readings = await GetAsync<IEnumerable<ReadingResult>>("api/v1/readings");

        // Assert
        Assert.NotNull(readings);
        var readingsList = readings.ToList();
        Assert.Equal(2, readingsList.Count);

        var reading1 = readingsList.First(r => r.Id == config1.Id);
        Assert.Equal(config1.Id, reading1.Id);
        Assert.True(reading1.Value >= config1.MinValue);
        Assert.True(reading1.Value <= config1.MaxValue);
        Assert.NotEqual(0, reading1.Value); // Ensure we're getting actual readings

        var reading2 = readingsList.First(r => r.Id == config2.Id);
        Assert.Equal(config2.Id, reading2.Id);
        Assert.True(reading2.Value >= config2.MinValue);
        Assert.True(reading2.Value <= config2.MaxValue);
        Assert.NotEqual(0, reading2.Value); // Ensure we're getting actual readings
    }

    [Fact]
    public async Task GetReadings_WithVirtualSensor_ReturnsValidReadings()
    {
        // Arrange
        var physicalConfig = new SensorConfig
        {
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

        var virtualConfig = new SensorConfig
        {
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

        // Act
        var readings = await GetAsync<IEnumerable<ReadingResult>>("api/v1/readings");

        // Assert
        Assert.NotNull(readings);
        var readingsList = readings.ToList();
        Assert.Equal(2, readingsList.Count);

        // Validate virtual sensor reading
        var virtualReading = readingsList.First(r => r.Id == virtualConfig.Id);
        Assert.NotNull(virtualReading);
        Assert.True(virtualReading.Value >= virtualConfig.MinValue);
        Assert.True(virtualReading.Value <= virtualConfig.MaxValue);
        Assert.NotEqual(0, virtualReading.Value); // Ensure we're getting actual readings

        // Validate that virtual reading is derived from physical reading
        var physicalReading = readingsList.First(r => r.Id == physicalConfig.Id);
        Assert.Equal(physicalReading.Value * 0.8, virtualReading.Value);
    }

    [Fact]
    public async Task GetReadings_WithConfiguredSensors_ReturnsReadingsForAllSensors()
    {
        // Arrange
        var configs = new List<SensorConfig>
        {
            new SensorConfig
            {
                Id = "sensor1",
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
                Id = "sensor2",
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

        await PostSensorConfigsWithDelay(configs);

        // Act
        var readings = await GetAsync<IEnumerable<ReadingResult>>("api/v1/readings");

        // Assert
        Assert.NotNull(readings);
        var readingsList = readings.ToList();
        Assert.Equal(2, readingsList.Count);
        Assert.Contains(readingsList, r => r.Id == "sensor1");
        Assert.Contains(readingsList, r => r.Id == "sensor2");
    }

    [Fact]
    public async Task GetReading_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var sensorId = "nonexistent_sensor";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(async () =>
            await GetAsync<ReadingResult>($"api/v1/readings/{sensorId}"));
        
        Assert.Contains("404", exception.Message);
    }

    [Fact]
    public async Task GetReading_WithInvalidIdFormat_ReturnsBadRequest()
    {
        // Act
        var response = await GetWithFullResponse("api/v1/readings/invalid!id");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetReading_WithConfiguredSensor_ReturnsValidReading()
    {
        // Arrange
        var config = new SensorConfig
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
        };

        await PostSensorConfigsWithDelay(new List<SensorConfig> { config });

        // Act
        var response = await GetAsync<ReadingResult>($"api/v1/readings/{config.Id}");

        // Assert
        Assert.NotNull(response);
        Assert.Equal(config.Id, response.Id);
        Assert.True(response.Value >= config.MinValue);
        Assert.True(response.Value <= config.MaxValue);
    }

    [Fact]
    public async Task GetReading_WithInvalidId_ReturnsBadRequest()
    {
        // Act
        var response = await Client.GetAsync("api/v1/readings/invalid-id");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetReading_AfterSensorRemoved_ReturnsNotFound()
    {
        // Arrange
        var sensorConfig = new SensorConfig
        {
            Id = "temp_sensor",
            Name = "Temperature Sensor",
            Type = SensorType.Temperature,
            Unit = "°C",
            Channel = 0,
            MinVoltage = 0,
            MaxVoltage = 3.3,
            MinValue = -40,
            MaxValue = 125
        };

        await PostSensorConfigsWithDelay(new List<SensorConfig> { sensorConfig });

        // Remove the sensor by updating with an empty list
        await PostSensorConfigsWithDelay(new List<SensorConfig>());

        // Act
        var response = await GetWithFullResponse($"api/v1/readings/{sensorConfig.Id}");
        
        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
