using Microsoft.Extensions.Hosting;

namespace TestLibrary
{
    public static class Class1
    {
        public static void AAA(this IHostBuilder builder)
        {
            builder.ConfigureServices(x => { });
        }
    }
}
