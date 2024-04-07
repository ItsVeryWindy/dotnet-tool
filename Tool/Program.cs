// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using YamlDotNet.Serialization;
using System.Diagnostics;

var assemblyPath = args[0];
var nugetPath = args[1];

Console.WriteLine($"Hello, World! {assemblyPath} {nugetPath}");

var context = new CustomAssemblyLoadContext(assemblyPath, nugetPath);

//Debugger.Launch();

var resolver = HostFactoryResolver.ResolveServiceProviderFactory(context.Assembly)(Array.Empty<string>());

var options = resolver.GetService<IOptions<HealthCheckServiceOptions>>();

if (options is null)
    return;

foreach (var reg in options.Value.Registrations)
{
    Console.WriteLine($"Health Check Name: {reg.Name}");
}
