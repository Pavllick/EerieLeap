using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EerieLeap.Controllers.Filters;

public class ValidationExceptionFilter : IAsyncActionFilter {
    private static readonly Regex _arrayIndexPattern = new(@"^\$\[\d+\]$", RegexOptions.Compiled);

    public async Task OnActionExecutionAsync(ActionExecutingContext context, [Required] ActionExecutionDelegate next) {
        if (!context.ModelState.IsValid && context.ActionDescriptor.Parameters.Count != context.ActionArguments.Count) {
            var parameterNames = context.ActionDescriptor.Parameters.Select(p => p.Name).ToHashSet();
            var filteredModelState = new ModelStateDictionary();

            foreach (var state in context.ModelState) {
                if (_arrayIndexPattern.IsMatch(state.Key)) {
                    var problemDetails = new ProblemDetails {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Invalid request format",
                        Detail = "The request contains invalid JSON format. Please verify your JSON syntax and data types."
                    };

                    context.Result = new BadRequestObjectResult(problemDetails);
                    return;
                }

                if (!parameterNames.Contains(state.Key)) {
                    foreach (var error in state.Value.Errors) {
                        string sanitizedKey = state.Key.Replace("$", string.Empty, StringComparison.OrdinalIgnoreCase);
                        if (sanitizedKey.Length > 0 && sanitizedKey[0] == '.')
                            sanitizedKey = sanitizedKey[1..];

                        filteredModelState.AddModelError(sanitizedKey, error.ErrorMessage);
                    }
                }
            }

            context.Result = new BadRequestObjectResult(new ValidationProblemDetails(filteredModelState));

            return;

        }

        if (!context.ModelState.IsValid) {
            context.Result = new BadRequestObjectResult(new ValidationProblemDetails(context.ModelState));

            return;
        }

        await next().ConfigureAwait(false);
    }
}
