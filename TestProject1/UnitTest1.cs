using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using WebApplication2;

namespace TestProject1
{
    public class L : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>
    {
        private IDisposable? _disposable;

        public void OnCompleted()
        {
            _disposable?.Dispose();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnNext(DiagnosticListener value)
        {
            if (value.Name == "Microsoft.Extensions.Hosting")
            {
                _disposable = value.Subscribe(this);
            }
        }

        public void OnNext(KeyValuePair<string, object?> value)
        {
            if (value.Key == "HostBuilt")
            {
                throw new HostAbortedException();
            }
        }
    }

    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            var factory = WebHostBuilderFactory.CreateFromTypesAssemblyEntryPoint<Program>(Array.Empty<string>());

            var f = new WebApplicationFactory<Program>();

            var s = f.Server.Services;

            var l = new L();

            using var subscriber = DiagnosticListener.AllListeners.Subscribe(l);

            _ = typeof(Program).Assembly.EntryPoint!.Invoke(null, new object[] { Array.Empty<string>() });

            Assert.Pass();
        }
    }
}