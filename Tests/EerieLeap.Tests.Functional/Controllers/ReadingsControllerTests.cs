using System.Net;
using EerieLeap.Domain.SensorDomain.Models;
using EerieLeap.Tests.Functional.Infrastructure;
using EerieLeap.Tests.Functional.Models;
using Xunit;
using Xunit.Abstractions;

namespace EerieLeap.Tests.Functional.Controllers;

[Collection("Sequential")]
public class ReadingsControllerTests : FunctionalTestBase, IAsyncLifetime {
    private readonly ITestOutputHelper _output;

    public ReadingsControllerTests(ITestOutputHelper output) =>
        _output = output;

    public async Task InitializeAsync() =>
        await ConfigureAdc();

    [Fact]
    public async Task GetReadings_ReturnsSuccessStatusCode() {
        // Act
        var readings = await GetAsync<IEnumerable<SensorReading>>("api/v1/readings");

        // Assert
        Assert.NotNull(readings);
    }

    // TODO: Sometimes fails
    [Fact]
    public async Task GetReadings_WithPhysicalSensor_ReturnsValidReadings() {
        // Arrange
        var config1 = SensorConfigRequest.CreateValidPhysical() with {
            Id = "physical_sensor_1",
            Channel = 0,
            MinValue = 1
        };

        var config2 = SensorConfigRequest.CreateValidPhysical() with {
            Id = "physical_sensor_2",
            Channel = 1,
            MinValue = 1
        };

        await PostSensorConfigsWithDelay(new List<SensorConfigRequest> { config1, config2 });

        // Act
        var readings = await GetAsync<IEnumerable<SensorReading>>("api/v1/readings");

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
    public async Task GetReadings_WithVirtualSensor_ReturnsValidReadings() {
        // Arrange
        var physicalConfig = SensorConfigRequest.CreateValidPhysical() with { Id = "physical_temp_1" };

        var virtualConfig = SensorConfigRequest.CreateValidVirtual() with {
            Id = "virtual_avg_temp",
            MinValue = 1,
            ConversionExpression = "{physical_temp_1} * 0.8" // Simple scaling of the physical sensor
        };

        await PostSensorConfigsWithDelay(new List<SensorConfigRequest> { physicalConfig, virtualConfig });

        // Act
        var readings = await GetAsync<IEnumerable<SensorReading>>("api/v1/readings");

        // Assert
        Assert.NotNull(readings);
        var readingsList = readings.ToList();
        Assert.Equal(2, readingsList.Count);

        // Validate virtual sensor reading
        var virtualReading = readingsList.First(r => r.Id == virtualConfig.Id);
        Assert.NotNull(virtualReading);
        Assert.NotEqual(0, virtualReading.Value); // Ensure we're getting actual readings

        // Validate that virtual reading is derived from physical reading
        var physicalReading = readingsList.First(r => r.Id == physicalConfig.Id);
        Assert.Equal(physicalReading.Value * 0.8, virtualReading.Value);
    }

    [Fact]
    public async Task GetReadings_WithConfiguredSensors_ReturnsReadingsForAllSensors() {
        // Arrange
        var configs = new List<SensorConfigRequest>
        {
            SensorConfigRequest.CreateValidPhysical() with { Id = "sensor1", Channel = 0 },
            SensorConfigRequest.CreateValidPhysical() with { Id = "sensor2", Channel = 1 }
        };

        await PostSensorConfigsWithDelay(configs);

        // Act
        var readings = await GetAsync<IEnumerable<SensorReading>>("api/v1/readings");

        // Assert
        Assert.NotNull(readings);
        var readingsList = readings.ToList();
        Assert.Equal(2, readingsList.Count);
        Assert.Contains(readingsList, r => r.Id == "sensor1");
        Assert.Contains(readingsList, r => r.Id == "sensor2");
    }

    [Fact]
    public async Task GetReading_WithInvalidId_ReturnsNotFound() {
        // Arrange
        var sensorId = "nonexistent_sensor";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(async () =>
            await GetAsync<SensorReading>($"api/v1/readings/{sensorId}"));

        Assert.Contains("404", exception.Message);
    }

    [Fact]
    public async Task GetReading_WithInvalidIdFormat_ReturnsBadRequest() {
        // Act
        var response = await GetWithFullResponse("api/v1/readings/invalid!id");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetReading_WithConfiguredSensor_ReturnsValidReading() {
        // Arrange
        var config = SensorConfigRequest.CreateValidPhysical();

        await PostSensorConfigsWithDelay(new List<SensorConfigRequest> { config });

        // Act
        var response = await GetAsync<SensorReading>($"api/v1/readings/{config.Id}");

        // Assert
        Assert.NotNull(response);
        Assert.Equal(config.Id, response.Id);
        Assert.True(response.Value >= config.MinValue);
        Assert.True(response.Value <= config.MaxValue);
    }

    [Fact]
    public async Task GetReading_WithInvalidId_ReturnsBadRequest() {
        // Act
        var response = await Client.GetAsync("api/v1/readings/invalid-id");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetReading_AfterSensorRemoved_ReturnsNotFound() {
        // Arrange
        var sensorConfig = SensorConfigRequest.CreateValidPhysical();

        await PostSensorConfigsWithDelay(new List<SensorConfigRequest> { sensorConfig });

        // Remove the sensor by updating with an empty list
        await PostSensorConfigsWithDelay(new List<SensorConfigRequest>());

        // Act
        var response = await GetWithFullResponse($"api/v1/readings/{sensorConfig.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    public Task DisposeAsync() {
        Dispose();
        return Task.CompletedTask;
    }
}
