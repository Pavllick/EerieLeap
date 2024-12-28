using Mono.Cecil;

namespace ValidationProcessor;

public class Program {
    static void Main(string[] args) {
        if (args.Length < 2) {
            Console.WriteLine("Usage: ILInjector <input-assembly> <output-assembly>");
            return;
        }

        string inputAssembly = args[0];
        string outputAssembly = args[1];

        Console.WriteLine("Updating assembly: " + inputAssembly);

        var resolver = new AssemblyResolver(inputAssembly);

        var readerParameters = new ReaderParameters {
            AssemblyResolver = resolver
        };

        // Load the input assembly
        var assembly = AssemblyDefinition.ReadAssembly(inputAssembly, readerParameters);

        var assemblyValidator = new AssemblyValidator(assembly.MainModule);
        assemblyValidator.Execute();

        assembly.Write(outputAssembly);
    }
}
