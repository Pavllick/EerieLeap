using System.ComponentModel.DataAnnotations;
using EerieLeap.Configuration;
using EerieLeap.Types;

namespace EerieLeap.Utilities.DataAnnotations;

/// <summary>
/// Validation attribute that makes a property required only for physical (non-virtual) sensors.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class RequiredForPhysicalSensorAttribute : ValidationAttribute {
    protected override ValidationResult? IsValid(object? value, [Required] ValidationContext validationContext) {
        if (validationContext.ObjectInstance is not SensorConfig sensorConfig)
            return new ValidationResult("This attribute can only be used on SensorConfig properties",
                new[] { validationContext.MemberName ?? string.Empty });

        if (sensorConfig.Type != SensorType.Virtual && value == null)
            return new ValidationResult(
                ErrorMessage ?? $"The {validationContext.DisplayName} field is required for physical sensors.",
                new[] { validationContext.MemberName ?? string.Empty });

        return ValidationResult.Success;
    }
}
