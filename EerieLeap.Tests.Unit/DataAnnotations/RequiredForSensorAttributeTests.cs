using System.ComponentModel.DataAnnotations;
using EerieLeap.Configuration;
using EerieLeap.Types;
using EerieLeap.Utilities.DataAnnotations;
using Xunit;

namespace EerieLeap.Tests.Unit.DataAnnotations;

public class RequiredForSensorAttributeTests
{
    private class TestModel
    {
        [RequiredForPhysicalSensor]
        public double? PhysicalValue { get; set; }

        [RequiredForVirtualSensor]
        public string? VirtualValue { get; set; }
    }

    [Theory]
    [InlineData(SensorType.Physical, null, false, "PhysicalValue")]
    [InlineData(SensorType.Physical, 42.0, true, "PhysicalValue")]
    [InlineData(SensorType.Virtual, null, true, "PhysicalValue")]
    [InlineData(SensorType.Virtual, 42.0, true, "PhysicalValue")]
    public void RequiredForPhysicalSensor_ValidatesCorrectly(SensorType sensorType, double? value, bool expectedValid, string propertyName)
    {
        // Arrange
        var model = new TestModel { PhysicalValue = value };
        var sensorConfig = new SensorConfig { Type = sensorType };
        var context = new ValidationContext(sensorConfig) 
        { 
            MemberName = propertyName,
            DisplayName = propertyName
        };
        var attribute = new RequiredForPhysicalSensorAttribute();

        // Act
        var result = attribute.GetValidationResult(model.PhysicalValue, context);

        // Assert
        Assert.Equal(expectedValid, result == ValidationResult.Success);
        if (!expectedValid)
        {
            Assert.Contains("required for physical sensors", result!.ErrorMessage);
            Assert.Equal(propertyName, result.MemberNames.First());
        }
    }

    [Theory]
    [InlineData(SensorType.Virtual, null, false, "VirtualValue")]
    [InlineData(SensorType.Virtual, "", false, "VirtualValue")]
    [InlineData(SensorType.Virtual, "expression", true, "VirtualValue")]
    [InlineData(SensorType.Physical, null, true, "VirtualValue")]
    [InlineData(SensorType.Physical, "", true, "VirtualValue")]
    public void RequiredForVirtualSensor_ValidatesCorrectly(SensorType sensorType, string? value, bool expectedValid, string propertyName)
    {
        // Arrange
        var model = new TestModel { VirtualValue = value };
        var sensorConfig = new SensorConfig { Type = sensorType };
        var context = new ValidationContext(sensorConfig)
        {
            MemberName = propertyName,
            DisplayName = propertyName
        };
        var attribute = new RequiredForVirtualSensorAttribute();

        // Act
        var result = attribute.GetValidationResult(model.VirtualValue, context);

        // Assert
        Assert.Equal(expectedValid, result == ValidationResult.Success);
        if (!expectedValid)
        {
            Assert.Contains("required for virtual sensors", result!.ErrorMessage);
            Assert.Equal(propertyName, result.MemberNames.First());
        }
    }

    [Fact]
    public void RequiredForPhysicalSensor_OnNonSensorConfig_ThrowsValidationError()
    {
        // Arrange
        var model = new TestModel();
        var context = new ValidationContext(model)
        {
            MemberName = "PhysicalValue",
            DisplayName = "PhysicalValue"
        };
        var attribute = new RequiredForPhysicalSensorAttribute();

        // Act
        var result = attribute.GetValidationResult(model.PhysicalValue, context);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("can only be used on SensorConfig properties", result.ErrorMessage);
    }

    [Fact]
    public void RequiredForVirtualSensor_OnNonSensorConfig_ThrowsValidationError()
    {
        // Arrange
        var model = new TestModel();
        var context = new ValidationContext(model)
        {
            MemberName = "VirtualValue",
            DisplayName = "VirtualValue"
        };
        var attribute = new RequiredForVirtualSensorAttribute();

        // Act
        var result = attribute.GetValidationResult(model.VirtualValue, context);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("can only be used on SensorConfig properties", result.ErrorMessage);
    }

    [Fact]
    public void RequiredForPhysicalSensor_WithCustomErrorMessage_UsesCustomMessage()
    {
        // Arrange
        var customMessage = "Custom error message for physical sensor.";
        var model = new TestModel { PhysicalValue = null };
        var sensorConfig = new SensorConfig { Type = SensorType.Physical };
        var context = new ValidationContext(sensorConfig)
        {
            MemberName = "PhysicalValue",
            DisplayName = "PhysicalValue"
        };
        var attribute = new RequiredForPhysicalSensorAttribute { ErrorMessage = customMessage };

        // Act
        var result = attribute.GetValidationResult(model.PhysicalValue, context);

        // Assert
        Assert.Equal(customMessage, result!.ErrorMessage);
    }

    [Fact]
    public void RequiredForVirtualSensor_WithCustomErrorMessage_UsesCustomMessage()
    {
        // Arrange
        var customMessage = "Custom error message for virtual sensor.";
        var model = new TestModel { VirtualValue = null };
        var sensorConfig = new SensorConfig { Type = SensorType.Virtual };
        var context = new ValidationContext(sensorConfig)
        {
            MemberName = "VirtualValue",
            DisplayName = "VirtualValue"
        };
        var attribute = new RequiredForVirtualSensorAttribute { ErrorMessage = customMessage };

        // Act
        var result = attribute.GetValidationResult(model.VirtualValue, context);

        // Assert
        Assert.Equal(customMessage, result!.ErrorMessage);
    }
}
