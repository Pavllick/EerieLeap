using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EerieLeap.Utilities.Filters;

public class ModelStateValidationFilter : IActionFilter {
    public void OnActionExecuting(ActionExecutingContext context) {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.ModelState.IsValid) {
            context.Result = new BadRequestObjectResult(
                new ValidationProblemDetails(context.ModelState));
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
