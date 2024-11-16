using AutoSensorMonitor.Configuration;
using AutoSensorMonitor.Types;
using AutoSensorMonitor.Validation;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace AutoSensorMonitor.Tests.Unit.Validation;

public class ValidationAttributeTests
{
    [Fact]
    public void RequiredForPhysicalSensor_WhenPhysicalSensorAndPropertyNull_ValidationFails()
    {
        // Arrange
        var model = new SensorConfig { Type = SensorType.Temperature };
        var validationContext = new ValidationContext(model);
        var attribute = new RequiredForPhysicalSensorAttribute();

        // Act
        var result = attribute.GetValidationResult(null, validationContext);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.ErrorMessage ?? string.Empty);
    }

    [Fact]
    public void RequiredForPhysicalSensor_WhenPhysicalSensorAndPropertySet_ValidationPasses()
    {
        // Arrange
        var model = new SensorConfig { Type = SensorType.Temperature, Channel = 1 };
        var validationContext = new ValidationContext(model);
        var attribute = new RequiredForPhysicalSensorAttribute();

        // Act
        var result = attribute.GetValidationResult(1, validationContext);

        // Assert
        Assert.Equal(ValidationResult.Success, result);
    }

    [Fact]
    public void RequiredForPhysicalSensor_WhenVirtualSensor_ValidationPasses()
    {
        // Arrange
        var model = new SensorConfig { Type = SensorType.Virtual };
        var validationContext = new ValidationContext(model);
        var attribute = new RequiredForPhysicalSensorAttribute();

        // Act
        var result = attribute.GetValidationResult(null, validationContext);

        // Assert
        Assert.Equal(ValidationResult.Success, result);
    }

    [Fact]
    public void RequiredForVirtualSensor_WhenVirtualSensorAndPropertyNull_ValidationFails()
    {
        // Arrange
        var model = new SensorConfig { Type = SensorType.Virtual };
        var validationContext = new ValidationContext(model);
        var attribute = new RequiredForVirtualSensorAttribute();

        // Act
        var result = attribute.GetValidationResult(null, validationContext);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.ErrorMessage ?? string.Empty);
    }

    [Fact]
    public void RequiredForVirtualSensor_WhenVirtualSensorAndPropertySet_ValidationPasses()
    {
        // Arrange
        var model = new SensorConfig { Type = SensorType.Virtual, ConversionExpression = "2 * x" };
        var validationContext = new ValidationContext(model);
        var attribute = new RequiredForVirtualSensorAttribute();

        // Act
        var result = attribute.GetValidationResult("2 * x", validationContext);

        // Assert
        Assert.Equal(ValidationResult.Success, result);
    }

    [Fact]
    public void RequiredForVirtualSensor_WhenPhysicalSensor_ValidationPasses()
    {
        // Arrange
        var model = new SensorConfig { Type = SensorType.Temperature };
        var validationContext = new ValidationContext(model);
        var attribute = new RequiredForVirtualSensorAttribute();

        // Act
        var result = attribute.GetValidationResult(null, validationContext);

        // Assert
        Assert.Equal(ValidationResult.Success, result);
    }
}
