using Microsoft.Extensions.DependencyInjection;
using Finbourne.GenericCache.Memory;
using Serilog;
using Finbourne.GenericCache.Core.Interface;
using Serilog.Core;

namespace Finbourne.GenericCache.Example
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var serviceProvider = SetupDI();
            var cache = serviceProvider.GetService<IGenericCache>();
            // You could also do
            // var cache = new MemoryCacheCore();
            // naturally dependency injection should be preferred in real applications

            await cache.SubscribeDeleteAsync<string>((key, reason, value) =>
            {
                Console.WriteLine($"Key {key} was deleted because {reason}.");
            }); 

            await cache.SetAsync("key1", "value1");
            await cache.SetAsync("key2", "value2");
            await cache.TryGetAsync("key1", out string value1);
            Console.WriteLine($"Value of key1 is {value1}");
            await cache.SetAsync("key3", "value3");
            if(!await cache.TryGetAsync("key2", out string value1_2))
            {
                Console.WriteLine($"Value of key2 is not in cache, because it was correctly removed");
            }
            Console.ReadLine();
        }

        private static ServiceProvider SetupDI()
        {
            var services = new ServiceCollection();
            Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

            services.AddLogging(builder =>
            {
                builder.AddSerilog(dispose: true); // dispose: true will dispose the Serilog logger when the provider is disposed
            });
            services.AddMemoryCache(new MemoryCacheConfig
            {
                MaxSize = 2
            });
            var serviceProvider = services.BuildServiceProvider();
            return serviceProvider;
        }
    }
}