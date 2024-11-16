using Metalama.Framework.Aspects;
using System.ComponentModel.DataAnnotations;

namespace AutoSensorMonitor.Aspects;

public class ValidateAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        foreach (var parameter in meta.Target.Parameters)
        {
            if (parameter.Value != null)
            {
                var validationContext = new ValidationContext(parameter.Value);
                Validator.ValidateObject(parameter.Value, validationContext, validateAllProperties: true);
            }
            else if (parameter.Attributes.OfAttributeType(typeof(RequiredAttribute)).Any())
            {
                throw new ValidationException($"Parameter '{parameter.Name}' cannot be null.");
            }
        }

        return meta.Proceed();
    }
}
