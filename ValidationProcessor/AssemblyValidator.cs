using System.ComponentModel.DataAnnotations;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace ValidationProcessor;

public class AssemblyValidator {
    public ModuleDefinition ModuleDefinition { get; }

    public AssemblyValidator(ModuleDefinition moduleDefinition) =>
        ModuleDefinition = moduleDefinition;

    public void Execute() {
        foreach (var type in ModuleDefinition.Types.Where(t => !t.CustomAttributes.Any(a => a.AttributeType.FullName.Contains("IgnoreCustomValidation"))))
            ProcessType(type);
    }

    private void ProcessType(TypeDefinition type) {
        // Logging to build output
        // WriteMessage("Weaving method: " + type.FullName, MessageImportance.High);

        // Process constructors
        foreach (var method in type.GetConstructors().Where(m => m.HasParameters && m.HasBody))
            ProcessMethod(method);

        // Process methods
        foreach (var method in type.Methods.Where(m => m.HasParameters && m.HasBody))
            ProcessMethod(method);

        // Process properties and fields
        foreach (var property in type.Properties.Where(p => p.CustomAttributes.Any()))
            ProcessProperty(property);

        foreach (var field in type.Fields.Where(f => f.CustomAttributes.Any()))
            ProcessField(field);
    }

    private void ProcessMethod(MethodDefinition method) {
        if (!method.HasBody)
            return;

        var il = method.Body.GetILProcessor();
        var firstInstruction = method.Body.Instructions.First();

        foreach (var param in method.Parameters.Where(p => p.CustomAttributes.Any(IsRequiredAttribute)))
            InjectRequiredParameterValidation(il, firstInstruction, param);

        foreach (var param in method.Parameters.Where(p => p.CustomAttributes.Any(IsValidationAttribute)))
            InjectObjectValidation(il, firstInstruction, param);
    }

    private void ProcessProperty(PropertyDefinition property) {
        if (property.SetMethod == null)
            return;

        var il = property.SetMethod.Body.GetILProcessor();
        var firstInstruction = property.SetMethod.Body.Instructions.First();

        if (property.CustomAttributes.Any(IsRequiredAttribute))
            InjectRequiredParameterValidation(il, firstInstruction, property.SetMethod.Parameters.First());

        var valueParameter = property.SetMethod.Parameters.First();
        InjectPropertyValidation(il, firstInstruction, property, valueParameter);
    }

    private void ProcessField(FieldDefinition field) {
        var setterMethod = field.DeclaringType.Methods
            .FirstOrDefault(m => m.Name == "set_" + field.Name);

        if (setterMethod == null)
            return;

        var il = setterMethod.Body.GetILProcessor();
        var firstInstruction = setterMethod.Body.Instructions.First();

        if (field.CustomAttributes.Any(IsRequiredAttribute))
            InjectRequiredParameterValidation(il, firstInstruction, setterMethod.Parameters.First());

        InjectFieldValidation(il, firstInstruction, field);
    }

    //private bool IsRequiredAttribute(CustomAttribute attribute) =>
    //    string.Equals(
    //        //ModuleDefinition.ImportReference(typeof(RequiredAttribute)).Resolve().FullName,
    //        //"System.ComponentModel.DataAnnotations.RequiredAttribute",
    //        typeof(RequiredAttribute).FullName,
    //        attribute.AttributeType.FullName,
    //        StringComparison.InvariantCulture);

    private bool IsRequiredAttribute(CustomAttribute attribute) {
        //WriteMessage("Weaving method: " + typeof(RequiredAttribute).FullName + " != " + attribute.AttributeType.FullName, MessageImportance.High);

        return string.Equals(
            ModuleDefinition.ImportReference(typeof(RequiredAttribute)).Resolve().FullName,
            //"System.ComponentModel.DataAnnotations.RequiredAttribute",
            //typeof(RequiredAttribute).FullName,
            attribute.AttributeType.FullName,
            StringComparison.InvariantCulture);
    }

    private bool IsValidationAttribute(CustomAttribute attribute) =>
        string.Equals(
            ModuleDefinition.ImportReference(typeof(ValidationAttribute)).Resolve().FullName,
            //"System.ComponentModel.DataAnnotations.ValidationAttribute",
            //typeof(ValidationAttribute).FullName,
            attribute.AttributeType.Resolve().BaseType?.FullName,
            StringComparison.InvariantCulture);

    private void InjectRequiredParameterValidation(ILProcessor il, Instruction firstInstruction, ParameterDefinition param) {
        // Ensure the parameter is nullable
        if (!IsNullableType(param.ParameterType))
            return;

        // Emit: ArgumentNullException.ThrowIfNull(param, nameof(param));

        // Load the parameter (arg0, arg1, etc.) onto the stack
        il.InsertBefore(firstInstruction, il.Create(OpCodes.Ldarg, param));

        // Load the parameter name (string) onto the stack
        il.InsertBefore(firstInstruction, il.Create(OpCodes.Ldstr, param.Name));

        // Call ArgumentNullException.ThrowIfNull(param, paramName)
        var throwIfNullMethod = GetThrowIfNullMethodReference();
        il.InsertBefore(firstInstruction, il.Create(OpCodes.Call, throwIfNullMethod));
    }

    private MethodReference GetThrowIfNullMethodReference() =>
        ModuleDefinition.ImportReference(typeof(ArgumentNullException).GetMethods()
            .First(m => m.Name == "ThrowIfNull"
                && m.GetParameters().Length == 2
                && m.GetParameters()[0].ParameterType.FullName == "System.Object"
                && m.GetParameters()[1].ParameterType.FullName == "System.String"));

    private void InjectObjectValidation(ILProcessor il, Instruction firstInstruction, ParameterDefinition parameter) {
        // Step 1: Load the parameter onto the stack
        il.InsertBefore(firstInstruction, il.Create(OpCodes.Ldarg, parameter)); // Load 'parameter' (Validator.ValidateObject parameter)

        // Step 2: Create a new ValidationContext instance
        il.InsertBefore(firstInstruction, il.Create(OpCodes.Ldarg, parameter)); // Load 'parameter' again for the ValidationContext
        var validationContextCtor = ModuleDefinition.ImportReference(typeof(ValidationContext).GetConstructor([typeof(object)]));
        il.InsertBefore(firstInstruction, il.Create(OpCodes.Newobj, validationContextCtor)); // new ValidationContext(parameter)

        // Step 3: Set MemberName property on the ValidationContext
        il.InsertBefore(firstInstruction, il.Create(OpCodes.Dup)); // Duplicate ValidationContext on the stack
        il.InsertBefore(firstInstruction, il.Create(OpCodes.Ldstr, parameter.Name)); // Load 'parameter.Name'
        var memberNameSetter = ModuleDefinition.ImportReference(
            typeof(ValidationContext).GetProperty(nameof(ValidationContext.MemberName)).SetMethod);
        il.InsertBefore(firstInstruction, il.Create(OpCodes.Callvirt, memberNameSetter)); // ValidationContext.MemberName = parameter.Name

        // Step 4: Load 'true' for validateAllProperties
        il.InsertBefore(firstInstruction, il.Create(OpCodes.Ldc_I4_1)); // true (validateAllProperties)

        // Step 5: Call Validator.ValidateObject
        var validateObjectMethod = ModuleDefinition.ImportReference(
            typeof(Validator).GetMethod(nameof(Validator.ValidateObject), [typeof(object), typeof(ValidationContext), typeof(bool)]));
        il.InsertBefore(firstInstruction, il.Create(OpCodes.Call, validateObjectMethod)); // Validator.ValidateObject(...)
    }

    private void InjectPropertyValidation(ILProcessor il, Instruction firstInstruction, PropertyDefinition property, ParameterDefinition valueParameter) {
        // Step 1: Load the value to validate
        il.InsertBefore(firstInstruction, il.Create(OpCodes.Ldarg, valueParameter)); // Load 'value' (Validator.ValidateProperty first parameter)

        // Step 2: Create a new ValidationContext instance with meta.This
        il.InsertBefore(firstInstruction, il.Create(OpCodes.Ldarg_0)); // Load 'this' (meta.This)
        var validationContextCtor = ModuleDefinition.ImportReference(typeof(ValidationContext).GetConstructor([typeof(object)]));
        il.InsertBefore(firstInstruction, il.Create(OpCodes.Newobj, validationContextCtor)); // new ValidationContext(meta.This)

        // Step 3: Set the MemberName property of ValidationContext
        il.InsertBefore(firstInstruction, il.Create(OpCodes.Dup)); // Duplicate ValidationContext on the stack
        il.InsertBefore(firstInstruction, il.Create(OpCodes.Ldstr, property.Name)); // Load 'Property.Name'
        var memberNameSetter = ModuleDefinition.ImportReference(
            typeof(ValidationContext).GetProperty(nameof(ValidationContext.MemberName)).SetMethod);
        il.InsertBefore(firstInstruction, il.Create(OpCodes.Callvirt, memberNameSetter)); // ValidationContext.MemberName = property.Name

        // Step 4: Call Validator.ValidateProperty
        var validatePropertyMethod = ModuleDefinition.ImportReference(
            typeof(Validator).GetMethod(nameof(Validator.ValidateProperty), [typeof(object), typeof(ValidationContext)]));
        il.InsertBefore(firstInstruction, il.Create(OpCodes.Call, validatePropertyMethod)); // Validator.ValidateProperty(value, ValidationContext)
    }

    private void InjectFieldValidation(ILProcessor il, Instruction firstInstruction, FieldDefinition field) {
        // Step 1: Load the field value to validate
        il.InsertBefore(firstInstruction, il.Create(OpCodes.Ldarg_0)); // Load 'this' (meta.This)
        il.InsertBefore(firstInstruction, il.Create(OpCodes.Ldfld, field)); // Load the field value

        // Step 2: Create a new ValidationContext instance with meta.This
        var validationContextCtor = ModuleDefinition.ImportReference(typeof(ValidationContext).GetConstructor([typeof(object)]));
        il.InsertBefore(firstInstruction, il.Create(OpCodes.Newobj, validationContextCtor)); // new ValidationContext(meta.This)

        // Step 3: Set the MemberName property of ValidationContext
        il.InsertBefore(firstInstruction, il.Create(OpCodes.Dup)); // Duplicate ValidationContext on the stack
        il.InsertBefore(firstInstruction, il.Create(OpCodes.Ldstr, field.Name)); // Load field name
        var memberNameSetter = ModuleDefinition.ImportReference(
            typeof(ValidationContext).GetProperty(nameof(ValidationContext.MemberName)).SetMethod);
        il.InsertBefore(firstInstruction, il.Create(OpCodes.Callvirt, memberNameSetter)); // ValidationContext.MemberName = field.Name

        // Step 4: Call Validator.ValidateProperty
        var validatePropertyMethod = ModuleDefinition.ImportReference(
            typeof(Validator).GetMethod(nameof(Validator.ValidateProperty), [typeof(object), typeof(ValidationContext)]));
        il.InsertBefore(firstInstruction, il.Create(OpCodes.Call, validatePropertyMethod)); // Validator.ValidateProperty(fieldValue, ValidationContext)
    }

    /// <summary>
    /// Checks if the given parameter type is nullable.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is nullable, otherwise false.</returns>
    private bool IsNullableType(TypeReference type) =>
        type.IsValueType
            ? type.IsGenericInstance && type.FullName.StartsWith("System.Nullable`1")
            : true;
}
