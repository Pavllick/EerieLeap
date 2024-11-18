using EerieLeap.Configuration;
using EerieLeap.Types;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace EerieLeap.Tests.Unit.Configuration;

public class SensorConfigTests
{
    [Fact]
    public void SensorConfig_WhenCreated_ShouldHaveDefaultValues()
    {
        // Act
        var config = new SensorConfig();

        // Assert
        Assert.NotEmpty(config.Id);
        Assert.Equal(string.Empty, config.Name);
        Assert.Equal(string.Empty, config.Unit);
        Assert.Equal(1000, config.SamplingRateMs);
    }

    [Fact]
    public void SensorConfig_WhenNameSet_ShouldGenerateId()
    {
        // Arrange
        var config = new SensorConfig { Id = string.Empty };
        config.Name = "Test Sensor";
        var firstId = "test_sensor"; // Expected ID based on name

        // Act
        config.Name = "Different Sensor";

        // Assert
        Assert.Equal(firstId, config.Id); // ID should not change since it was already set
        Assert.NotEmpty(config.Id);
    }

    [Theory]
    [InlineData("test@sensor")]
    [InlineData("test#123")]
    public void Validate_WithInvalidId_ShouldFailValidation(string invalidId)
    {
        // Arrange
        var config = new SensorConfig
        {
            Id = invalidId,
            Name = "Test",
            Type = SensorType.Temperature,
            Unit = "C",
            Channel = 0,
            MinVoltage = 0,
            MaxVoltage = 5,
            MinValue = 0,
            MaxValue = 100
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(config);
        var isValid = Validator.TryValidateObject(config, context, validationResults, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(validationResults, r => r.MemberNames.Contains("Id") && 
            r.ErrorMessage != null && r.ErrorMessage.Contains("can only contain letters, numbers, and underscores"));
    }

    [Fact]
    public void Validate_PhysicalSensorWithoutRequiredProperties_ShouldFailValidation()
    {
        // Arrange
        var config = new SensorConfig
        {
            Id = "test_sensor",
            Name = "Test Sensor",
            Type = SensorType.Temperature,
            Unit = "C"
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(config);
        var isValid = Validator.TryValidateObject(config, context, validationResults, true);

        // Assert
        Assert.False(isValid);
        var errors = validationResults.SelectMany(r => r.MemberNames.Select(m => (m, r.ErrorMessage))).ToList();
        Assert.Contains(errors, e => e.Item1 == "Channel" && e.Item2 == "The Channel field is required for physical sensors.");
        Assert.Contains(errors, e => e.Item1 == "MinVoltage" && e.Item2 == "The MinVoltage field is required for physical sensors.");
        Assert.Contains(errors, e => e.Item1 == "MaxVoltage" && e.Item2 == "The MaxVoltage field is required for physical sensors.");
        Assert.Contains(errors, e => e.Item1 == "MinValue" && e.Item2 == "The MinValue field is required for physical sensors.");
        Assert.Contains(errors, e => e.Item1 == "MaxValue" && e.Item2 == "The MaxValue field is required for physical sensors.");
    }

    [Fact]
    public void Validate_VirtualSensorWithoutPhysicalProperties_ShouldPassValidation()
    {
        // Arrange
        var config = new SensorConfig
        {
            Id = "virtual_sensor_1",
            Name = "Virtual Sensor 1",
            Type = SensorType.Virtual,
            ConversionExpression = "2 * {temp_sensor_1}",
            Unit = "C"
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(config);
        var isValid = Validator.TryValidateObject(config, context, validationResults, true);

        // Assert
        Assert.Empty(validationResults);
        Assert.True(isValid);
    }

    [Fact]
    public void Validate_VirtualSensorWithoutExpression_ShouldFailValidation()
    {
        // Arrange
        var config = new SensorConfig
        {
            Id = "virtual_sensor_1",
            Name = "Virtual Sensor 1",
            Type = SensorType.Virtual,
            Unit = "C"
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(config);
        var isValid = Validator.TryValidateObject(config, context, validationResults, true);

        // Assert
        Assert.False(isValid);
        
        var errors = validationResults.SelectMany(r => r.MemberNames.Select(m => (m, r.ErrorMessage))).ToList();

        Assert.Contains(errors, e => e.Item1 == "ConversionExpression" && 
            e.Item2 == "Virtual sensors must have a conversion expression to combine other sensor values");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(32)]
    public void Validate_PhysicalSensorWithInvalidChannel_ShouldFailValidation(int invalidChannel)
    {
        // Arrange
        var config = new SensorConfig
        {
            Id = "temp_sensor_1",
            Name = "Temperature Sensor 1",
            Type = SensorType.Temperature,
            Channel = invalidChannel,
            MinVoltage = 0,
            MaxVoltage = 5,
            MinValue = 0,
            MaxValue = 100,
            Unit = "C"
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(config);
        var isValid = Validator.TryValidateObject(config, context, validationResults, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(validationResults, r => r.MemberNames.Contains("Channel"));
    }
}
