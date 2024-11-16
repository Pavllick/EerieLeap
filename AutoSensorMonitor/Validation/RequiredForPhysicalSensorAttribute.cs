using System.ComponentModel.DataAnnotations;
using AutoSensorMonitor.Configuration;
using AutoSensorMonitor.Types;

namespace AutoSensorMonitor.Validation;

/// <summary>
/// Validation attribute that makes a property required only for physical (non-virtual) sensors.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class RequiredForPhysicalSensorAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var sensorConfig = validationContext.ObjectInstance as SensorConfig;
        if (sensorConfig == null)
            return new ValidationResult("This attribute can only be used on SensorConfig properties",
                new[] { validationContext.MemberName ?? string.Empty });

        if (sensorConfig.Type != SensorType.Virtual && value == null)
            return new ValidationResult(
                ErrorMessage ?? $"The {validationContext.DisplayName} field is required for physical sensors.",
                new[] { validationContext.MemberName ?? string.Empty });

        return ValidationResult.Success;
    }
}
