using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;
using System.Reflection;
using System.Runtime.Loader;

class CustomAssemblyLoadContext : AssemblyLoadContext
{
    private readonly CompositeCompilationAssemblyResolver _resolvers;
    private readonly DependencyContext _context;

    public Assembly Assembly { get; }

    public CustomAssemblyLoadContext(string assemblyPath, string nugetPath)
    {
        var assemblyDirectory = Path.GetDirectoryName(assemblyPath) ?? throw new ArgumentException("Could not load assembly directory");
        
        _resolvers = new CompositeCompilationAssemblyResolver(new ICompilationAssemblyResolver[]
        {
            new AppBaseCompilationAssemblyResolver(assemblyDirectory),
            new ReferenceAssemblyPathResolver(),
            new PackageCompilationAssemblyResolver(nugetPath)
        });

        Assembly = LoadFromAssemblyPath(assemblyPath);

        _context = DependencyContext.Load(Assembly) ?? throw new ArgumentException("Could not load dependency context for assembly");
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var compilationLibrary = _context.CompileLibraries.FirstOrDefault(runtime => string.Equals(runtime.Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase));

        var assemblies = new List<string>();

        if (compilationLibrary is not null && _resolvers.TryResolveAssemblyPaths(compilationLibrary, assemblies) && assemblies.Any())
            return LoadFromAssemblyPath(assemblies.First());

        var runtimeLibrary = _context.RuntimeLibraries.FirstOrDefault(runtime => string.Equals(runtime.Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase));

        if (runtimeLibrary is not null)
        {
            compilationLibrary = new CompilationLibrary(runtimeLibrary.Type, runtimeLibrary.Name, runtimeLibrary.Version, runtimeLibrary.Hash, runtimeLibrary.RuntimeAssemblyGroups.SelectMany(g => g.AssetPaths), runtimeLibrary.Dependencies, runtimeLibrary.Serviceable);

            if (_resolvers.TryResolveAssemblyPaths(compilationLibrary, assemblies) && assemblies.Any())
                return LoadFromAssemblyPath(assemblies.First());
        }

        return base.Load(assemblyName);
    }
}