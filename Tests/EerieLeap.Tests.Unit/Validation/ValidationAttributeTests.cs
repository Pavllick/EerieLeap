using EerieLeap.Configuration;
using EerieLeap.Domain.SensorDomain.Models;
using EerieLeap.Domain.SensorDomain.DataAnnotations;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace EerieLeap.Tests.Unit.Domain.SensorDomain.Validation;

public class ValidationAttributeTests {
    [Fact]
    public void RequiredForPhysicalSensor_WhenPhysicalSensorAndPropertyNull_ValidationFails() {
        // Arrange
        var model = new SensorConfig { Type = SensorType.Physical };
        var validationContext = new ValidationContext(model);
        var attribute = new RequiredForPhysicalSensorAttribute();

        // Act
        var result = attribute.GetValidationResult(null, validationContext);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.ErrorMessage ?? string.Empty);
    }

    [Fact]
    public void RequiredForPhysicalSensor_WhenPhysicalSensorAndPropertySet_ValidationPasses() {
        // Arrange
        var model = new SensorConfig { Type = SensorType.Physical, Channel = 1 };
        var validationContext = new ValidationContext(model);
        var attribute = new RequiredForPhysicalSensorAttribute();

        // Act
        var result = attribute.GetValidationResult(1, validationContext);

        // Assert
        Assert.Equal(ValidationResult.Success, result);
    }

    [Fact]
    public void RequiredForPhysicalSensor_WhenVirtualSensor_ValidationPasses() {
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
    public void RequiredForVirtualSensor_WhenVirtualSensorAndPropertyNull_ValidationFails() {
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
    public void RequiredForVirtualSensor_WhenVirtualSensorAndPropertySet_ValidationPasses() {
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
    public void RequiredForVirtualSensor_WhenPhysicalSensor_ValidationPasses() {
        // Arrange
        var model = new SensorConfig { Type = SensorType.Physical };
        var validationContext = new ValidationContext(model);
        var attribute = new RequiredForVirtualSensorAttribute();

        // Act
        var result = attribute.GetValidationResult(null, validationContext);

        // Assert
        Assert.Equal(ValidationResult.Success, result);
    }
}
