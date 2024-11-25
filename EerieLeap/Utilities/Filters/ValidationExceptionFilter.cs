using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EerieLeap.Utilities.Filters;

public class ValidationExceptionFilter : IExceptionFilter {
    public void OnException(ExceptionContext context) {
        if (context.Exception is ValidationException validationException) {
            context.Result = new BadRequestObjectResult(new ValidationProblemDetails {
                Title = "One or more validation errors occurred.",
                Status = StatusCodes.Status400BadRequest,
                Detail = validationException.Message,
                Errors = new Dictionary<string, string[]> {
                    { validationException.ValidationResult?.MemberNames.FirstOrDefault() ?? "Error", new[] { validationException.Message } }
                }
            });
            context.ExceptionHandled = true;
        }
    }
}
