using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace EerieLeap.Controllers.ModelBinders;

internal class RawStringModelBinderProvider : IModelBinderProvider {
    public IModelBinder GetBinder([Required] ModelBinderProviderContext context) {
        if (context.Metadata is DefaultModelMetadata metadata
            && metadata.ModelType == typeof(string)
            && metadata.Attributes.Attributes.OfType<JavaScriptContentTypeAttribute>().Any())

            return new JavaScriptModelBinder();

        return null;
    }
}
