using System;
using System.Collections.Generic;
using System.Linq;
using Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;

public class ValidationWeaver : BaseModuleWeaver {
    public override void Execute() {
        foreach (var type in ModuleDefinition.Types.Where(t => !t.CustomAttributes.Any(a => a.AttributeType.FullName.Contains("IgnoreCustomValidation"))))
            ProcessType(type);
    }

    private void ProcessType(TypeDefinition type) {
        // Logging to build output
        // WriteMessage("Weaving method: " + type.FullName, MessageImportance.High);

        // Process methods
        foreach (var method in type.Methods.Where(m => m.HasParameters && m.HasBody))
            ProcessMethod(method);

        // Process properties and fields
        foreach (var property in type.Properties.Where(p => p.CustomAttributes.Any(IsRequiredAttribute)))
            ProcessProperty(property);

        foreach (var field in type.Fields.Where(f => f.CustomAttributes.Any(IsRequiredAttribute)))
            ProcessField(field);
    }

    private void ProcessMethod(MethodDefinition method) {
        var il = method.Body.GetILProcessor();
        var firstInstruction = method.Body.Instructions.First();

        foreach (var param in method.Parameters.Where(p => p.CustomAttributes.Any(IsRequiredAttribute)))
            InjectRequiredParameterValidation(il, firstInstruction, param);
    }

    private void ProcessProperty(PropertyDefinition property) {
        if (property.SetMethod == null)
            return;

        var il = property.SetMethod.Body.GetILProcessor();
        var instructions = property.SetMethod.Body.Instructions;
        var firstInstruction = instructions.First();

        InjectRequiredParameterValidation(il, firstInstruction, property.SetMethod.Parameters.First());
    }

    private void ProcessField(FieldDefinition field) {
        // If the field has a setter method
        if (field.HasCustomAttributes) {
            var setterMethod = field.DeclaringType.Methods
                .FirstOrDefault(m => m.Name == "set_" + field.Name);

            if (setterMethod != null) {
                var il = setterMethod.Body.GetILProcessor();
                var firstInstruction = setterMethod.Body.Instructions.First();

                // Inject validation logic to the setter
                InjectRequiredParameterValidation(il, firstInstruction, setterMethod.Parameters.First());
            }
        }
    }

    private bool IsRequiredAttribute(CustomAttribute attribute) =>
        attribute.AttributeType.FullName.Contains(nameof(System.ComponentModel.DataAnnotations.RequiredAttribute));

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

    /// <summary>
    /// Checks if the given parameter type is nullable.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is nullable, otherwise false.</returns>
    private bool IsNullableType(TypeReference type) {
        if (type.IsGenericInstance && type.GetElementType() is GenericInstanceType genericType)
            return genericType.ElementType.FullName == "System.Nullable`1";

        if (!type.IsValueType)  
            return true;

        return false;
    }

    //private bool IsValidationAttribute(CustomAttribute attribute) =>
    //    attribute.AttributeType.FullName.Contains("ValidationAttribute"); // == typeof(ValidationAttribute).FullName;

    public override IEnumerable<string> GetAssembliesForScanning() => Array.Empty<string>();
}
