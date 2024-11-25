using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace EerieLeap.Utilities.Converters;

public class ValidationJsonConverterFactory {
    private readonly IActionContextAccessor _actionContextAccessor;

    public ValidationJsonConverterFactory(IActionContextAccessor actionContextAccessor) =>
        _actionContextAccessor = actionContextAccessor;

    public IEnumerable<JsonConverter> CreateConvertersForAssembly(Assembly assembly) {
        ArgumentNullException.ThrowIfNull(assembly);

        var typesToValidate = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsPublic)
            .Where(t => t.Namespace == "EerieLeap.Configuration");

        return typesToValidate.Select(type => {
            var converterType = typeof(ValidationJsonConverter<>).MakeGenericType(type);
            return (JsonConverter)Activator.CreateInstance(converterType, _actionContextAccessor)!;
        });
    }
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

        Console.WriteLine($"Deserializing {typeToConvert.Name}");

        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        var result = CreateInstance(root, typeToConvert, options);
        ArgumentNullException.ThrowIfNull(result);

        var modelState = _actionContextAccessor.ActionContext?.ModelState ?? new ModelStateDictionary();

        foreach (var property in typeToConvert.GetProperties()) {
            if (!property.CanWrite)
                continue;

            try {
                if (TryGetPropertyCaseInsensitive(root, property.Name, out var element)) {
                    var value = element.ValueKind == JsonValueKind.Null ?
                        null :
                        JsonSerializer.Deserialize(element.GetRawText(), property.PropertyType, options);

                    property.SetValue(result, value);
                } else {
                    property.SetValue(result, default);
                }
            } catch (TargetInvocationException ex) when (ex.InnerException is ArgumentException or InvalidOperationException or ValidationException) {
                if (ex.InnerException is ValidationException validationEx) {
                    modelState.AddModelError(property.Name, validationEx.Message);
                } else {
                    throw;
                }
            }
        }

        return !modelState.IsValid ? null : result;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, value, GetSanitizedOptions(options));

    private static T? CreateInstance(JsonElement root, Type typeToConvert, JsonSerializerOptions options) {
        var constructors = typeToConvert.GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length);

        foreach (var constructor in constructors) {
            var parameters = constructor.GetParameters();
            if (parameters.Length == 0)
                continue;

            var args = new object?[parameters.Length];
            var allParamsFound = true;

            for (var i = 0; i < parameters.Length; i++) {
                var param = parameters[i];
                if (!TryGetPropertyCaseInsensitive(root, param.Name!, out var element)) {
                    allParamsFound = false;
                    break;
                }

                args[i] = element.ValueKind == JsonValueKind.Null
                    ? null
                    : JsonSerializer.Deserialize(element.GetRawText(), param.ParameterType, options);
            }

            if (allParamsFound) {
                try {
                    return (T?)constructor.Invoke(args);
                } catch (TargetInvocationException ex) when (ex.InnerException is ArgumentException or InvalidOperationException or ValidationException) {
                    // Skip this constructor if it fails validation or argument checks
                    continue;
                }
            }
        }

        return Activator.CreateInstance<T>();
    }

    private static bool TryGetPropertyCaseInsensitive(JsonElement element, string propertyName, out JsonElement value) {
        foreach (var property in element.EnumerateObject()) {
            if (property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase)) {
                value = property.Value;
                return true;
            }
        }

        value = default;

        return false;
    }

    private static JsonSerializerOptions GetSanitizedOptions(JsonSerializerOptions options) {
        var sanitizedOptions = new JsonSerializerOptions(options);
        for (var i = sanitizedOptions.Converters.Count - 1; i >= 0; i--) {
            if (sanitizedOptions.Converters[i] is ValidationJsonConverter<T>)
                sanitizedOptions.Converters.RemoveAt(i);
        }
        return sanitizedOptions;
    }
}
