namespace ScriptInterpreter.Tests.Unit;

public class InterpreterTests : IDisposable {
    private Interpreter? _interpreter;

    public void Dispose() =>
        _interpreter?.Dispose();

    [Fact]
    public void UpdateScript_ValidScript_ShouldMapMethodAndExecute() {
        var concatMethodInfo = new MethodInfo {
            Name = "concatFunction",
            Parameters = [
                new ParameterInfo { Name = "str1", Type = typeof(string) },
                new ParameterInfo { Name = "str2", Type = typeof(string) }]
        };
        var sumMethodInfo = new MethodInfo {
            Name = "sumFunction",
            Parameters = [
                new ParameterInfo { Name = "a", Type = typeof(int) },
                new ParameterInfo { Name = "b", Type = typeof(int) }]
        };

        _interpreter = new Interpreter([concatMethodInfo, sumMethodInfo]);

        string jsScript = @"
            function concatFunction(str1, str2) {
                return str1 + str2;
            }
            function sumFunction(a, b) {
                return a + b;
            }
        ";

        _interpreter.UpdateScript(jsScript);

        Assert.True(concatMethodInfo.IsAvailable);
        Assert.True(sumMethodInfo.IsAvailable);

        // Execute the function and check the return type
        var concatResult = concatMethodInfo.Execute("Hello, ", "World!");
        Assert.Equal("Hello, World!", concatResult);

        var sumResult = sumMethodInfo.Execute(2, 3);
        Assert.Equal(5, sumResult);
    }

    [Fact]
    public void Execute_GenericMethod_ShouldReturnExpectedType() {
        var concatMethodInfo = new MethodInfo<string> {
            Name = "concatFunction",
            Parameters = [
                new ParameterInfo { Name = "str1", Type = typeof(string) },
                new ParameterInfo { Name = "str2", Type = typeof(string) }]
        };
        var sumMethodInfo = new MethodInfo<int> {
            Name = "sumFunction",
            Parameters = [
                new ParameterInfo { Name = "a", Type = typeof(int) },
                new ParameterInfo { Name = "b", Type = typeof(int) }]
        };

        _interpreter = new Interpreter([concatMethodInfo, sumMethodInfo]);

        string jsScript = @"
            function concatFunction(str1, str2) {
                return str1 + str2;
            }
            function sumFunction(a, b) {
                return a + b;
            }
        ";

        _interpreter.UpdateScript(jsScript);

        Assert.True(concatMethodInfo.IsAvailable);
        Assert.True(sumMethodInfo.IsAvailable);

        // Execute the function and check the return type
        string concatResult = concatMethodInfo.Execute("Hello, ", "World!");
        Assert.Equal("Hello, World!", concatResult);

        int sumResult = sumMethodInfo.Execute(2, 3);
        Assert.Equal(5, sumResult);
    }

    [Fact]
    public void GenericExecute_Method_ShouldReturnExpectedType() {
        var concatMethodInfo = new MethodInfo {
            Name = "concatFunction",
            Parameters = [
                new ParameterInfo { Name = "str1", Type = typeof(string) },
                new ParameterInfo { Name = "str2", Type = typeof(string) }]
        };
        var sumMethodInfo = new MethodInfo {
            Name = "sumFunction",
            Parameters = [
                new ParameterInfo { Name = "a", Type = typeof(int) },
                new ParameterInfo { Name = "b", Type = typeof(int) }]
        };

        _interpreter = new Interpreter([concatMethodInfo, sumMethodInfo]);

        string jsScript = @"
            function concatFunction(str1, str2) {
                return str1 + str2;
            }
            function sumFunction(a, b) {
                return a + b;
            }
        ";

        _interpreter.UpdateScript(jsScript);

        Assert.True(concatMethodInfo.IsAvailable);
        Assert.True(sumMethodInfo.IsAvailable);

        // Execute the function and check the return type
        string concatResult = concatMethodInfo.Execute<string>("Hello, ", "World!");
        Assert.Equal("Hello, World!", concatResult);

        int sumResult = sumMethodInfo.Execute<int>(2, 3);
        Assert.Equal(5, sumResult);
    }

    [Fact]
    public void UpdateScript_AddsMethodDynamicallyAndExecutes() {
        _interpreter = new Interpreter(Array.Empty<IMethodInfo>());

        var dynamicMethodInfo = new MethodInfo<double> {
            Name = "dynamicFunction",
            Parameters = [new ParameterInfo { Name = "x", Type = typeof(double) }]
        };

        string jsScript = @"
            function dynamicFunction(x) {
                return x * x;
            }
        ";

        _interpreter.UpdateScript(jsScript);

        _interpreter.AddMethod(dynamicMethodInfo);

        Assert.True(dynamicMethodInfo.IsAvailable);

        // Execute the dynamically added function
        double result = dynamicMethodInfo.Execute(4.0);
        Assert.Equal(16, result);
    }

    [Fact]
    public void UpdateScript_RequriedMethodNotFound_ShouldThrowException() {
        var methodInfo = new MethodInfo {
            Name = "nonExistentFunction",
            Parameters = Array.Empty<ParameterInfo>()
        };

        _interpreter = new Interpreter([methodInfo]);

        string jsScript = @"
            function someOtherFunction() {}
        ";

        Assert.Throws<InvalidOperationException>(() => _interpreter.UpdateScript(jsScript));
    }

    [Fact]
    public void Execute_OptionalUnavailableMethod_ShouldThrowException() {
        var methodInfo = new MethodInfo {
            Name = "nonExistentFunction",
            Parameters = Array.Empty<ParameterInfo>(),
            IsOptional = true
        };

        _interpreter = new Interpreter([methodInfo]);

        string jsScript = @"
            function someOtherFunction() {}
        ";

        _interpreter.UpdateScript(jsScript);

        Assert.False(methodInfo.IsAvailable);

        Assert.Throws<InvalidOperationException>(() => methodInfo.Execute());
    }

    [Fact]
    public void Execute_InvalidArgumentType_ShouldThrowException() {
        var methodInfo = new MethodInfo<double> {
            Name = "doubleFunction",
            Parameters = [new ParameterInfo { Name = "x", Type = typeof(double) }]
        };

        _interpreter = new Interpreter([methodInfo]);

        string jsScript = @"
            function doubleFunction(x) {
                return x * x;
            }
        ";

        _interpreter.UpdateScript(jsScript);

        Assert.True(methodInfo.IsAvailable);

        double result = methodInfo.Execute(4.0);
        Assert.Equal(16, result);

        Assert.Throws<ArgumentException>(() => methodInfo.Execute(4));
    }

    [Fact]
    public void Execute_MissingArguments_ShouldThrowException() {
        var methodInfo = new MethodInfo<double> {
            Name = "doubleFunction",
            Parameters = [new ParameterInfo { Name = "x", Type = typeof(double) }]
        };

        _interpreter = new Interpreter([methodInfo]);

        string jsScript = @"
            function doubleFunction(x) {
                return x * x;
            }
        ";

        _interpreter.UpdateScript(jsScript);

        Assert.True(methodInfo.IsAvailable);

        double result = methodInfo.Execute(4.0);
        Assert.Equal(16, result);

        Assert.Throws<ArgumentException>(() => methodInfo.Execute());
    }

    [Fact]
    public void UpdateScript_RemoveExpectedFunction_ShouldNotBeAvailable() {
        var methodInfo = new MethodInfo {
            Name = "tempFunction",
            Parameters = Array.Empty<ParameterInfo>(),
            IsOptional = true
        };

        _interpreter = new Interpreter([methodInfo]);

        string initialScript = @"
            function tempFunction() {}
        ";

        _interpreter.UpdateScript(initialScript);
        Assert.True(methodInfo.IsAvailable);

        string updatedScript = @"
            // tempFunction is now removed
        ";

        _interpreter.UpdateScript(updatedScript);

        Assert.False(methodInfo.IsAvailable);
    }

    [Fact]
    public void UpdateScript_RemoveAddedFunction_ShouldRaiseMethodRemovedEvent() {
        var methodInfo = new MethodInfo {
            Name = "tempFunction",
            Parameters = Array.Empty<ParameterInfo>(),
            IsOptional = true
        };

        _interpreter = new Interpreter([]);

        bool methodRemovedEventRaised = false;

        _interpreter.MethodRemoved += removedMethod => {
            if (removedMethod.Name == "tempFunction") {
                methodRemovedEventRaised = true;
            }
        };

        string initialScript = @"
            function tempFunction() {}
        ";

        _interpreter.UpdateScript(initialScript);
        _interpreter.AddMethod(methodInfo);

        Assert.True(methodInfo.IsAvailable);

        string updatedScript = @"
            // tempFunction is now removed
        ";

        _interpreter.UpdateScript(updatedScript);

        Assert.False(methodInfo.IsAvailable);
        Assert.True(methodRemovedEventRaised);
    }
}
