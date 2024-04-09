using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Reflection;
using ToolLibrary.Abstractions;

namespace ToolLibrary
{
    public class Class1 : IClass1
    {
        public string[]? Do(Assembly assembly)
        {
            var resolver = HostFactoryResolver.ResolveServiceProviderFactory(assembly)(Array.Empty<string>());

            var options = resolver.GetService<IOptions<HealthCheckServiceOptions>>();

            if (options is null)
                return null;

            return options.Value.Registrations.Select(x => x.Name).ToArray();
        }
    }
}
