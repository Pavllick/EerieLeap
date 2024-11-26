using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Metalama.Framework.Diagnostics;
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
            .SelectMany(compilation => compilation.AllTypes)
            .SelectMany(type => type.AllMethods)
            .Where(method => method.Parameters.Any(p => p.Attributes.Any(a => a.Type.Is(typeof(ValidationAttribute)))))
            .AddAspect(target => new MethodValidationAspect());

        // Add validation to all constructors that have parameters with validation attributes
        amender
            .SelectMany(compilation => compilation.AllTypes)
            // .SelectMany(type => type.Constructors)
            .Where(type => type.Constructors.Any()
                && type.Constructors.Any(c => c.Parameters.Any(p => p.Attributes.Any(a => a.Type.Is(typeof(ValidationAttribute))))))
            .AddAspect(target => new ConstructorValidationAspect());

        // Add validation to all fields and properties with validation attributes
        amender
            .SelectMany(compilation => compilation.AllTypes)
            // Need to exclude EerieLeap.Configuration namespace because API validation is handled separately
            .Where(type => !type.ContainingNamespace.FullName.StartsWith("EerieLeap.Configuration", StringComparison.OrdinalIgnoreCase))
            .SelectMany(type => type.FieldsAndProperties)
            .Where(prop => prop.Attributes.Any(a => a.Type.Is(typeof(ValidationAttribute))))
            .AddAspect(target => new FieldOrPropertyValidationAspect(target));
    }

    // Suppress "CA1062:Validate arguments of public methods"
    private static readonly SuppressionDefinition _validateArgumentsOfPublicMethodsError = new("CA1062");

    [CompileTime]
    private sealed class ConstructorValidationAspect : TypeAspect {

        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "The builder parameter is provided by the Metalama framework and is never null")]
        public override void BuildAspect(IAspectBuilder<INamedType> builder) {
            foreach (var constructor in builder.Target.Constructors) {
                builder.With(constructor).Override(nameof(this.OverrideConstructorTemplate));

                foreach (var parameter in constructor.Parameters) {
                    if (parameter.Attributes.All(a => a.Type.Is(typeof(RequiredAttribute))))
                        builder.Diagnostics.Suppress(_validateArgumentsOfPublicMethodsError);
                }
            }
        }

        [Template]
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Method must be instance to be used as a template by Metalama")]
        private void OverrideConstructorTemplate() {
            foreach (var parameter in meta.Target.Parameters.Where(p => p.Attributes.Any(a => a.Type.Is(typeof(ValidationAttribute))))) {
                var isRequired = parameter.Attributes.Any(a => a.Type.Is(typeof(RequiredAttribute)));

                if (isRequired)
                    ArgumentNullException.ThrowIfNull(parameter.Value, parameter.Name);

                if (parameter.Value != null) {
                    Validator.ValidateObject(
                        parameter.Value,
                        new ValidationContext(parameter.Value) {
                            MemberName = parameter.Name
                        },
                        validateAllProperties: true);
                }
            }

            meta.Proceed();
        }
    }

    [CompileTime]
    private sealed class MethodValidationAspect : OverrideMethodAspect {
        // Suppress "CA1062:Validate arguments of public methods"
        private static readonly SuppressionDefinition _validateArgumentsOfPublicMethodsError = new("CA1062");

        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "The builder parameter is provided by the Metalama framework and is never null")]
        public override void BuildAspect(IAspectBuilder<IMethod> builder) {
            base.BuildAspect(builder);

            foreach (var parameter in builder.Target.Parameters) {
                if (parameter.Attributes.All(a => a.Type.Is(typeof(RequiredAttribute))))
                    builder.Diagnostics.Suppress(_validateArgumentsOfPublicMethodsError);
            }
        }

        public override dynamic? OverrideMethod() {
            foreach (var parameter in meta.Target.Parameters.Where(p => p.Attributes.Any(a => a.Type.Is(typeof(ValidationAttribute))))) {
                var isRequired = parameter.Attributes.Any(a => a.Type.Is(typeof(RequiredAttribute)));

                if (isRequired)
                    ArgumentNullException.ThrowIfNull(parameter.Value, parameter.Name);

                if (parameter.Value != null) {
                    Validator.ValidateObject(
                        parameter.Value,
                        new ValidationContext(parameter.Value) {
                            MemberName = parameter.Name
                        },
                        validateAllProperties: true);
                }
            }

            return meta.Proceed();
        }
    }

    [CompileTime]
    private sealed class FieldOrPropertyValidationAspect : OverrideFieldOrPropertyAspect {
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
                if (_isRequired)
                    ArgumentNullException.ThrowIfNull(value, Property.Name);

                if (value != null) {
                    Validator.ValidateProperty(
                        value,
                        new ValidationContext(meta.This) {
                            MemberName = Property.Name
                        });
                }

                meta.Proceed();
            }
        }
    }
}
