using System.ComponentModel.DataAnnotations;
using EerieLeap.Configuration;
using EerieLeap.Types;

namespace EerieLeap.Utilities.DataAnnotations;

/// <summary>
/// Validation attribute that makes a property required only for virtual sensors.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class RequiredForVirtualSensorAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var sensorConfig = validationContext.ObjectInstance as SensorConfig;
        if (sensorConfig == null)
            return new ValidationResult("This attribute can only be used on SensorConfig properties",
                new[] { validationContext.MemberName ?? string.Empty });

        if (sensorConfig.Type == SensorType.Virtual && string.IsNullOrEmpty(value?.ToString()))
            return new ValidationResult(
                ErrorMessage ?? $"The {validationContext.DisplayName} field is required for virtual sensors.",
                new[] { validationContext.MemberName ?? string.Empty });

        return ValidationResult.Success;
    }
}
