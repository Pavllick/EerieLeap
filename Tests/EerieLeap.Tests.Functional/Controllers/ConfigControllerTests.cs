using EerieLeap.Configuration;
using EerieLeap.Tests.Functional.Infrastructure;
using EerieLeap.Tests.Functional.Models;
using EerieLeap.Types;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Xunit;
using Xunit.Abstractions;

namespace EerieLeap.Tests.Functional.Controllers;

public class ConfigControllerTests : FunctionalTestBase {
    private readonly ITestOutputHelper _output;

    public ConfigControllerTests(TestWebApplicationFactory factory, ITestOutputHelper output)
        : base(factory) {
        _output = output;
    }

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
        var config = await GetAsync<AdcConfigRequest>("api/v1/config/adc");

        // Assert
        Assert.NotNull(config);
    }

    [Fact]
    public async Task GetSensorConfigs_ReturnsSuccessStatusCode() {
        // Act
        var configs = await GetAsync<List<SensorConfigRequest>>("api/v1/config/sensors");

        // Assert
        Assert.NotNull(configs);
    }

    [Fact]
    public async Task UpdateAdcConfig_WithValidAdcConfig_ReturnsSuccessStatusCode() {
        // Arrange
        var request = AdcConfigRequest.CreateValid();

        // Act
        var response = await PostAsync("api/v1/config/adc", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify all properties were updated correctly
        var updatedConfig = await GetAsync<AdcConfigRequest>("api/v1/config/adc");
        Assert.Equal(request.Type, updatedConfig!.Type);
        Assert.Equal(request.Resolution, updatedConfig.Resolution);
        Assert.Equal(request.ReferenceVoltage, updatedConfig.ReferenceVoltage);
        Assert.Equal(request.ClockFrequency, updatedConfig.ClockFrequency);
        Assert.Equal(request.BusId, updatedConfig.BusId);
        Assert.Equal(request.ChipSelect, updatedConfig.ChipSelect);
        Assert.Equal(request.Mode, updatedConfig.Mode);
        Assert.Equal(request.DataBitLength, updatedConfig.DataBitLength);
        Assert.NotNull(updatedConfig.Protocol);
    }

    [Fact]
    public async Task UpdateAdcConfig_WithInvalidJsonAdcConfig_ReturnsBadRequest() {
        // Arrange
        var jsonContent = @"{
            ""ChipSelect"": 0,
            ""Resolution"": 12
        }";

        // Act
        var response = await PostWithFullResponse("api/v1/config/adc", jsonContent);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Type", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BusId", content, StringComparison.OrdinalIgnoreCase);

        // Turns out validation list can contain "config" property,
        // which is a parameter name in the controller POST method
        Assert.DoesNotContain("config", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateAdcConfig_WithInvalidObjectAdcConfig_ReturnsBadRequest() {
        // Arrange
        var request = new AdcConfigRequest {
            ChipSelect = 0,
            Resolution = 12
        };

        // Act
        var response = await PostWithFullResponse("api/v1/config/adc", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Type", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BusId", content, StringComparison.OrdinalIgnoreCase);

        // Turns out validation list can contain "config" property,
        // which is a parameter name in the controller POST method
        Assert.DoesNotContain("config", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateSensorConfigs_WithValidConfigs_ReturnsSuccessStatusCode() {
        // Arrange
        var sensorConfigs = new List<SensorConfigRequest>
        {
            SensorConfigRequest.CreateValidPhysical() with { Id = "test_sensor_1", Channel = 0 },
            SensorConfigRequest.CreateValidPhysical() with { Id = "test_sensor_2", Channel = 1 }
        };

        // Act
        var response = await PostAsync("api/v1/config/sensors", sensorConfigs);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify the update
        var updatedConfigs = await GetAsync<List<SensorConfigRequest>>("api/v1/config/sensors");
        Assert.Contains(updatedConfigs!, c => c.Id == "test_sensor_1");
        Assert.Contains(updatedConfigs!, c => c.Id == "test_sensor_2");
    }

    [Fact]
    public async Task UpdateSensorConfigs_WithValidPhysicalSensor_ValidatesAllProperties() {
        // Arrange
        var request1 = SensorConfigRequest.CreateValidPhysical() with {
            Id = "physical_sensor_1"
        };

        var request2 = SensorConfigRequest.CreateValidPhysical() with {
            Id = "physical_sensor_2",
            Channel = 1,
            ConversionExpression = "x + 10"
        };

        var sensorConfigs = new List<SensorConfigRequest> { request1, request2 };

        await PostSensorConfigsWithDelay(sensorConfigs);

        // Act
        var storedConfigs = await GetAsync<IEnumerable<SensorConfigRequest>>("api/v1/config/sensors");

        // Validate sensor config was stored correctly
        var storedConfig1 = storedConfigs!.First(c => c.Id == request1.Id);
        Assert.Equal(request1.Name, storedConfig1.Name);
        Assert.Equal(request1.Type, storedConfig1.Type);
        Assert.Equal(request1.Unit, storedConfig1.Unit);
        Assert.Equal(request1.Channel, storedConfig1.Channel);
        Assert.Equal(request1.MinVoltage, storedConfig1.MinVoltage);
        Assert.Equal(request1.MaxVoltage, storedConfig1.MaxVoltage);
        Assert.Equal(request1.MinValue, storedConfig1.MinValue);
        Assert.Equal(request1.MaxValue, storedConfig1.MaxValue);
        Assert.Equal(request1.SamplingRateMs, storedConfig1.SamplingRateMs);
        Assert.Null(storedConfig1.ConversionExpression);

        var storedConfig2 = storedConfigs!.First(c => c.Id == request2.Id);
        Assert.Equal(request2.Name, storedConfig2.Name);
        Assert.Equal(request2.Type, storedConfig2.Type);
        Assert.Equal(request2.Unit, storedConfig2.Unit);
        Assert.Equal(request2.Channel, storedConfig2.Channel);
        Assert.Equal(request2.MinVoltage, storedConfig2.MinVoltage);
        Assert.Equal(request2.MaxVoltage, storedConfig2.MaxVoltage);
        Assert.Equal(request2.MinValue, storedConfig2.MinValue);
        Assert.Equal(request2.MaxValue, storedConfig2.MaxValue);
        Assert.Equal(request2.SamplingRateMs, storedConfig2.SamplingRateMs);
        Assert.Equal(request2.ConversionExpression, storedConfig2.ConversionExpression);
    }

    [Fact]
    public async Task UpdateSensorConfigs_WithValidVirtualSensor_ValidatesAllProperties() {
        // Arrange
        var request = SensorConfigRequest.CreateValidVirtual();

        // Act
        var response = await PostAsync("api/v1/config/sensors", new List<SensorConfigRequest> { request });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify all properties were updated correctly
        var updatedConfigs = await GetAsync<List<SensorConfigRequest>>("api/v1/config/sensors");
        var updatedConfig = updatedConfigs!.First(c => c.Id == request.Id);
        Assert.Equal(request.Name, updatedConfig.Name);
        Assert.Equal(request.Type, updatedConfig.Type);
        Assert.Equal(request.Unit, updatedConfig.Unit);
        Assert.Equal(request.SamplingRateMs, updatedConfig.SamplingRateMs);
        Assert.Equal(request.ConversionExpression, updatedConfig.ConversionExpression);
        Assert.Null(updatedConfig.Channel);
        Assert.Null(updatedConfig.MinVoltage);
        Assert.Null(updatedConfig.MaxVoltage);
        Assert.Null(updatedConfig.MinValue);
        Assert.Null(updatedConfig.MaxValue);
    }

    [Fact]
    public async Task UpdateSensorConfigs_WithInvalidConfig_ReturnsBadRequest() {
        // Arrange
        var request = new SensorConfigRequest {
            // Name field intentionally omitted
            Id = "invalid_sensor",
            Type = SensorType.Physical,
            Unit = "°C",
            Channel = 0,
            MinVoltage = 0.0,
            MaxVoltage = 3.3,
            MinValue = -40,
            MaxValue = 125,
            SamplingRateMs = 1000
        };

        // Act
        var response = await PostWithFullResponse("api/v1/config/sensors", new List<SensorConfigRequest> { request });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Name", content);
    }

    [Fact]
    public async Task UpdateSensorConfigs_WithInvalidConfigWithJson_ReturnsBadRequest() {
        // Arrange
        var jsonContent = @"[{
            ""id"": ""invalid_sensor"",
            ""type"": ""Physical"",
            ""unit"": ""°C"",
            ""channel"": 0,
            ""minVoltage"": 0.0,
            ""maxVoltage"": 3.3,
            ""minValue"": -40,
            ""maxValue"": 125,
            ""samplingRateMs"": 1000
        }]";

        // Act
        var response = await PostWithFullResponse("api/v1/config/sensors", jsonContent);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Name", content);
    }

    // TODO: Bug, returns config field is missing, which is a parameter name in the controller POST method
    // [Fact]
    // public async Task UpdateSensorConfigs_WithInvalidConfigWithJson_ReturnsBadRequest2() {
    //     // Arrange
    //     var jsonContent = @"[{
    //         ""id"": ""invalid_sensor"",
    //         ""type"": ""Temperature""
    //     }]";

    //     // Act
    //     var response = await PostWithFullResponse("api/v1/config/sensors", jsonContent);

    //     // Assert
    //     Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    //     var content = await response.Content.ReadAsStringAsync();
    //     Assert.Contains("Name", content);
    // }

    [Fact]
    public async Task UpdateSensorConfigs_WithDuplicateIds_ReturnsBadRequest() {
        // Arrange
        var request = SensorConfigRequest.CreateValidPhysical() with { Id = "duplicate_id" };
        var requests = new List<SensorConfigRequest> { request, request };

        // Act
        var response = await PostWithFullResponse("api/v1/config/sensors", requests);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("duplicate", content.ToLowerInvariant());
    }

    [Fact]
    public async Task GetSensorConfig_WithValidId_ReturnsConfig() {
        // Arrange
        var request = SensorConfigRequest.CreateValidPhysical();
        await PostAsync("api/v1/config/sensors", new List<SensorConfigRequest> { request });

        // Act
        var response = await GetAsync<SensorConfigRequest>($"api/v1/config/sensors/{request.Id}");

        // Assert
        Assert.NotNull(response);
        Assert.Equal(request.Id, response.Id);
    }

    [Fact]
    public async Task GetSensorConfig_WithInvalidId_ReturnsBadRequest() {
        // Act
        var response = await GetWithFullResponse("api/v1/config/sensors/nonexistent_id");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetSensorConfig_WithInvalidIdFormat_ReturnsBadRequest() {
        // Act
        var response = await GetWithFullResponse("api/v1/config/sensors/Invalid Id");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
