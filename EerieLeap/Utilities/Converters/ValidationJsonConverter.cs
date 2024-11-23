using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace EerieLeap.Utilities.Converters;

public class ValidationJsonConverterFactory {
    private readonly IActionContextAccessor _actionContextAccessor;

    public ValidationJsonConverterFactory(IActionContextAccessor actionContextAccessor) =>
        _actionContextAccessor = actionContextAccessor;

    public IEnumerable<JsonConverter> CreateConvertersForAssembly(Assembly assembly) {
        ArgumentNullException.ThrowIfNull(assembly);

        var typesToValidate = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsPublic)
            .Where(IsController);

        return typesToValidate.Select(type => {
            var converterType = typeof(ValidationJsonConverter<>).MakeGenericType(type);
            return (JsonConverter)Activator.CreateInstance(converterType, _actionContextAccessor)!;
        });
    }

    private static bool IsController(Type type) =>
        type.IsPublic &&
        !type.IsAbstract &&
        (type.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase) ||
         type.GetCustomAttributes<ControllerAttribute>(true).Any() ||
         type.GetCustomAttributes<ApiControllerAttribute>(true).Any()) &&
        (typeof(ControllerBase).IsAssignableFrom(type) ||
         typeof(Controller).IsAssignableFrom(type));
}

/// <summary>
/// JSON converter that ensures objects are created and populated in a way that triggers GlobalValidationFabric validation.
/// This works because GlobalValidationFabric adds validation at compile time to constructors and property setters.
/// </summary>
public class ValidationJsonConverter<T> : JsonConverter<T> where T : class {
    private readonly IActionContextAccessor _actionContextAccessor;

    public ValidationJsonConverter(IActionContextAccessor actionContextAccessor) =>
        _actionContextAccessor = actionContextAccessor;

    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        ArgumentNullException.ThrowIfNull(typeToConvert);

        var deserializedValue = JsonSerializer.Deserialize<T>(ref reader, GetSanitizedOptions(options));

        if (deserializedValue == null)
            return null;

        var constructor = typeToConvert
            .GetConstructors()
            .FirstOrDefault(c => c.GetParameters().Length > 0);

        var modelState = _actionContextAccessor.ActionContext?.ModelState ?? new ModelStateDictionary();
        T? result = null;

        if (constructor != null) {
            var constructorParams = constructor.GetParameters();
            var paramValues = constructorParams
                .Where(p => p.Name != null)
                .Select(param => deserializedValue.GetType().GetProperty(param.Name!)?.GetValue(deserializedValue))
                .ToArray();

            try {
                result = (T?)constructor.Invoke(paramValues);
            } catch (TargetInvocationException ex) {
                if (ex.InnerException is ValidationException validationEx) {
                    var failedParam = constructorParams
                        .FirstOrDefault(p => validationEx.Message.Contains(p.Name!, StringComparison.Ordinal))?.Name ?? "";
                    modelState.AddModelError(failedParam, validationEx.Message);
                } else {
                    throw;
                }
            }
        }

        result ??= Activator.CreateInstance<T>();
        ArgumentNullException.ThrowIfNull(result);

        foreach (var property in typeToConvert.GetProperties()) {
            if (property.CanWrite) {
                var sourceValue = deserializedValue.GetType().GetProperty(property.Name)?.GetValue(deserializedValue);

                try {
                    property.SetValue(result, sourceValue);
                } catch (TargetInvocationException ex) {
                    if (ex.InnerException is ValidationException validationEx) {
                        modelState.AddModelError(property.Name, validationEx.Message);
                    } else {
                        throw;
                    }
                }
            }
        }

        return !modelState.IsValid ? null : result;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, value, GetSanitizedOptions(options));

    private static JsonSerializerOptions GetSanitizedOptions(JsonSerializerOptions options) {
        var sanitizedOptions = new JsonSerializerOptions(options);
        for (var i = sanitizedOptions.Converters.Count - 1; i >= 0; i--) {
            if (sanitizedOptions.Converters[i] is ValidationJsonConverter<T>)
                sanitizedOptions.Converters.RemoveAt(i);
        }
        return sanitizedOptions;
    }
}
