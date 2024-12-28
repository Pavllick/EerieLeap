using Mono.Cecil;

namespace ValidationProcessor;

public class AssemblyResolver : DefaultAssemblyResolver {
    public AssemblyResolver(string inputAssemblyPath) {
        var inputDirectory = Path.GetDirectoryName(inputAssemblyPath);

        if (!string.IsNullOrEmpty(inputDirectory))
            AddSearchDirectory(inputDirectory);

        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => Path.GetDirectoryName(a.Location))
            .Distinct();

        foreach (var dir in loadedAssemblies)
            AddSearchDirectory(dir);

        var nugetPackages = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".nuget", "packages"
        );
        AddSearchDirectory(nugetPackages);

        // Debugging: Log all search directories
        Console.WriteLine("Search directories:");
        foreach (var dir in GetSearchDirectories())
            Console.WriteLine($"  {dir}");
    }

    public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters) {
        try {
            return base.Resolve(name, parameters);
        } catch (AssemblyResolutionException ex) {
            Console.WriteLine($"Error: Unable to resolve {name.FullName}. Details: {ex.Message}");
            throw;
        }
    }
}
