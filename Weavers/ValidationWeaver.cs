using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Weavers;

public class ValidationWeaver : BaseModuleWeaver {
    public override void Execute() {
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

        if(property.CustomAttributes.Any(IsRequiredAttribute))
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

    private bool IsRequiredAttribute(CustomAttribute attribute) =>
        string.Equals(
            ModuleDefinition.ImportReference(typeof(RequiredAttribute)).Resolve().FullName,
            attribute.AttributeType.FullName,
            StringComparison.InvariantCulture);

    private bool IsValidationAttribute(CustomAttribute attribute) =>
        string.Equals(
            ModuleDefinition.ImportReference(typeof(ValidationAttribute)).Resolve().FullName,
            attribute.AttributeType.Resolve().BaseType?.FullName,
            StringComparison.InvariantCulture);

    /// <summary>
    /// Injects argument null validation logic at the beginning of the method.
    /// </summary>
    /// <param name="il"></param>
    /// <param name="firstInstruction"></param>
    /// <param name="param"></param>
    private void InjectRequiredParameterValidation(ILProcessor il, Instruction firstInstruction, ParameterDefinition param) {
        // Emit: if (param == null) throw new ArgumentNullException(nameof(param));

        if (!IsNullableType(param.ParameterType))
            return;

        // Load the parameter onto the stack
        il.InsertBefore(firstInstruction, il.Create(OpCodes.Ldarg, param));

        // Compare parameter with null
        il.InsertBefore(firstInstruction, il.Create(OpCodes.Ldnull));
        il.InsertBefore(firstInstruction, il.Create(OpCodes.Ceq)); // Compare equality

        // Branch to the next instruction if not null
        var skipThrowInstruction = il.Create(OpCodes.Nop);
        il.InsertBefore(firstInstruction, il.Create(OpCodes.Brfalse_S, skipThrowInstruction));

        // Throw new ArgumentNullException(nameof(param))
        il.InsertBefore(firstInstruction, il.Create(OpCodes.Ldstr, param.Name)); // nameof(param)
        var ctor = GetArgumentNullExceptionConstructorReference();
        il.InsertBefore(firstInstruction, il.Create(OpCodes.Newobj, ctor)); // Create ArgumentNullException
        il.InsertBefore(firstInstruction, il.Create(OpCodes.Throw)); // Throw the exception

        // Add the skip instruction
        il.InsertBefore(firstInstruction, skipThrowInstruction);
    }

    private MethodReference GetArgumentNullExceptionConstructorReference() {
        var exceptionType = ModuleDefinition.ImportReference(typeof(ArgumentNullException));

        return ModuleDefinition.ImportReference(exceptionType.Resolve().Methods
            .First(m => m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == "System.String"));
    }

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

    public override IEnumerable<string> GetAssembliesForScanning() => ["System.ComponentModel.Annotations"]; // Array.Empty<string>();
}
