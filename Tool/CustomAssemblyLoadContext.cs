using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;
using System.Reflection;
using System.Runtime.Loader;
using ToolLibrary.Abstractions;

class CustomAssemblyLoadContext : AssemblyLoadContext
{
    private readonly CompositeCompilationAssemblyResolver _resolvers;
    private readonly AssemblyDependencyResolver _resolver;
    private readonly DependencyContext _context;

    public Assembly Assembly { get; }

    public CustomAssemblyLoadContext(string assemblyPath, string nugetPath)
    {
        var assemblyDirectory = Path.GetDirectoryName(assemblyPath) ?? throw new ArgumentException("Could not load assembly directory");
        
        _resolvers = new CompositeCompilationAssemblyResolver(
        [
            new AppBaseCompilationAssemblyResolver(assemblyDirectory),
            new ReferenceAssemblyPathResolver(),
            new PackageCompilationAssemblyResolver(nugetPath)
        ]);

        _resolver = new AssemblyDependencyResolver(assemblyPath);

        Assembly = LoadFromAssemblyPath(assemblyPath);

        _context = DependencyContext.Load(Assembly) ?? throw new ArgumentException("Could not load dependency context for assembly");
    }

    private static readonly string ToolLibraryAbstractionsName = typeof(IClass1).Assembly.GetName().Name!;

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        Console.WriteLine($"Resolving: {assemblyName}");

        var assembly = base.Load(assemblyName);

        if (assembly is not null)
            return assembly;

        var assemblyPath = ResolveAssemblyToPath(assemblyName);

        if (assemblyPath is not null)
        {
            Console.WriteLine($"Resolved to: {assemblyPath}");

            return LoadFromAssemblyPath(assemblyPath);
        }

        Console.WriteLine($"Could not resolve: {assemblyName}");

        return null;
    }

    private string? ResolveAssemblyToPath(AssemblyName assemblyName)
    {
        if (assemblyName.Name == ToolLibraryAbstractionsName)
            return null;

        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);

        if (assemblyPath is not null)
            return assemblyPath;

        assemblyPath = ResolveFromTrustedPlatformAssemblies(assemblyName);

        if (assemblyPath is not null)
            return assemblyPath;

        var compilationLibrary = _context.CompileLibraries.FirstOrDefault(runtime => string.Equals(runtime.Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase));

        var assemblyPaths = new List<string>();

        if (compilationLibrary is not null && _resolvers.TryResolveAssemblyPaths(compilationLibrary, assemblyPaths) && assemblyPaths.Any())
            return assemblyPaths.First();

        var runtimeLibrary = _context.RuntimeLibraries.FirstOrDefault(runtime => string.Equals(runtime.Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase));

        if (runtimeLibrary is null)
            return null;
        
        compilationLibrary = new CompilationLibrary(runtimeLibrary.Type, runtimeLibrary.Name, runtimeLibrary.Version, runtimeLibrary.Hash, runtimeLibrary.RuntimeAssemblyGroups.SelectMany(g => g.AssetPaths), runtimeLibrary.Dependencies, runtimeLibrary.Serviceable);

        if (_resolvers.TryResolveAssemblyPaths(compilationLibrary, assemblyPaths) && assemblyPaths.Any())
            return assemblyPaths.First();

        return null;
    }

    protected override nint LoadUnmanagedDll(string unmanagedDllName)
    {
        Console.WriteLine($"Resolving: {unmanagedDllName}");

        var handle = base.LoadUnmanagedDll(unmanagedDllName);

        if (handle != IntPtr.Zero)
            return handle;

        var dllPath = ResolveUnmanagedDllToPath(unmanagedDllName);

        if (dllPath is not null)
        {
            Console.WriteLine($"Resolved to: {dllPath}");

            return LoadUnmanagedDllFromPath(dllPath);
        }

        return 0;
    }

    private string? ResolveUnmanagedDllToPath(string unmanagedDllName)
    {
        var dllPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);

        if (dllPath is not null && File.Exists(dllPath))
            return dllPath;

        return ResolveFromNativeDllSearchDirectories(unmanagedDllName);
    }

    private string? ResolveFromTrustedPlatformAssemblies(AssemblyName assemblyName)
    {
        if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is not string data)
            return null;

        var assemblies = data.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

        foreach(var assembly in assemblies)
        {
            var fileName = Path.GetFileNameWithoutExtension(assembly);

            if (!fileName.Equals(assemblyName.Name, StringComparison.InvariantCultureIgnoreCase))
                continue;

            if (!File.Exists(assembly))
                continue;

            return assembly;
        }

        return null;
    }

    private string? ResolveFromNativeDllSearchDirectories(string unmanagedDllName)
    {
        if (AppContext.GetData("NATIVE_DLL_SEARCH_DIRECTORIES") is not string data)
            return null;

        var directories = data.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

        foreach (var directory in directories)
        {
            if (!Directory.Exists(directory))
                continue;

            var files = Directory.GetFiles(directory);

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);

                if (fileName == unmanagedDllName)
                    return file;
            }
        }

        return null;
    }
}
