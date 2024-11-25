using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Advising;

namespace EerieLeap.Aspects;

[CompileTime]
public sealed class GlobalValidationFabric : ProjectFabric {
    [SuppressMessage("Style", "IDE0016:Use 'throw' expression", Justification = "ThrowIfNull is not supported here")]
    [SuppressMessage("Maintainability", "CA1510:Use ArgumentNullException throw helper", Justification = "ThrowIfNull is not supported here")]
    public override void AmendProject(IProjectAmender amender) {
        if (amender == null)
            throw new ArgumentNullException(nameof(amender));

        // Add validation to all methods that have parameters with validation attributes
        amender
            .SelectMany(compilation => compilation.Types)
            .SelectMany(type => type.Methods)
            .Where(method => method.Parameters.Any(p => p.Attributes.Any(a => a.Type.Is(typeof(ValidationAttribute)))))
            .AddAspect(target => new MethodValidationAspect());

        // Add validation to all fields and properties with validation attributes
        amender
            .SelectMany(compilation => compilation.Types)
            .SelectMany(type => type.FieldsAndProperties)
            .Where(prop => prop.Attributes.Any(a => a.Type.Is(typeof(ValidationAttribute))))
            .AddAspect(target => new FieldOrPropertyValidationAspect(target));
    }
}

[CompileTime]
public sealed class MethodValidationAspect : OverrideMethodAspect {
    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "<Pending>")]
    public override void BuildAspect(IAspectBuilder<IMethod> builder) {
        var attribute = AttributeConstruction.Create(
            typeof(SuppressMessageAttribute),
            [
                "Design",
                "CA1062:Validate arguments of public methods"
            ],
            [
                new KeyValuePair<string, object?>("Justification", "Parameter validation is handled by MethodValidationAspect")
            ]);

        builder.With(builder.Target).IntroduceAttribute(attribute);

        base.BuildAspect(builder);
    }

    public override dynamic? OverrideMethod() {
        foreach (var parameter in meta.Target.Parameters) {
            if (parameter.Value != null) {
                Validator.ValidateObject(
                parameter.Value,
                new ValidationContext(parameter.Value),
                validateAllProperties: true);
            }
        }

        return meta.Proceed();
    }
}

[CompileTime]
public sealed class FieldOrPropertyValidationAspect : OverrideFieldOrPropertyAspect {
    private readonly bool _isRequired;

    public IFieldOrProperty Property { get; }

    [SuppressMessage("Style", "IDE0016:Use 'throw' expression", Justification = "ThrowIfNull is not supported here")]
    [SuppressMessage("Maintainability", "CA1510:Use ArgumentNullException throw helper", Justification = "ThrowIfNull is not supported here")]
    public FieldOrPropertyValidationAspect(IFieldOrProperty property) {
        if (property == null)
            throw new ArgumentNullException(nameof(property));

        Property = property;
        _isRequired = property.Attributes.Any(a => a.Type.Is(typeof(RequiredAttribute)));
    }

    public override dynamic? OverrideProperty {
        get => meta.Proceed();
        set {
            Validator.ValidateProperty(
                value,
                new ValidationContext(meta.This) {
                    MemberName = Property.Name
                });

            meta.Proceed();
        }
    }
}
