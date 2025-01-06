using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EerieLeap.Controllers.ModelBinders;

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
internal sealed class JavaScriptContentTypeAttribute : Attribute, IBindingSourceMetadata {
    public BindingSource BindingSource => BindingSource.Body;
}

internal class JavaScriptModelBinder : IModelBinder {
    public async Task BindModelAsync([Required] ModelBindingContext bindingContext) {
        var request = bindingContext.HttpContext.Request;

        if (request.ContentType?.StartsWith("application/javascript", StringComparison.OrdinalIgnoreCase) ?? false) {
            using (var reader = new StreamReader(request.Body)) {
                var content = await reader.ReadToEndAsync().ConfigureAwait(false);
                bindingContext.Result = ModelBindingResult.Success(content);

                return;
            }
        }

        bindingContext.Result = ModelBindingResult.Failed();
    }
}
