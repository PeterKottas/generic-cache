# Finbourne.GenericCache

Finbourne.GenericCache.Memory is a simple in-memory caching library for .NET. It provides a generic cache implementation that allows you to store and retrieve data with a limited capacity. When the cache reaches its capacity, the least recently used (LRU) item is automatically removed to make space for new entries.

In the future we should probably consider further CacheProviders such as Finbourne.GenericCache.Redis to expand the usefulness and functionality of this library.

## Installation

You can install the library from NuGet Package Manager Console or Visual Studio Package Manager Console using the following command:

```bash
Install-Package Finbourne.GenericCache.Memory
```

Or, if you're using .NET CLI, you can use the following command:
```
dotnet add package Finbourne.GenericCache.Memory
```

While I try to show extra credit, I didn't actually publish this. Please just grab the code from Github :-)

## Usage
First, you need to add the MemoryCacheCore and the required dependencies to the service collection in your application's Startup.cs:

```csharp
using Finbourne.GenericCache.Core.Interface;
using Finbourne.GenericCache.Memory;
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMemoryCache(new MemoryCacheConfig
        {
			MaxSize = 2
        });

        // Add any other services as needed
    }
}
```

We recommend using the convenience extensions method AddMemoryCache, but you can easily register components manually e.g.:

```csharp
services.AddSingleton<IGenericCache>(provider =>
{
    var cacheConfig = new MemoryCacheConfig { MaxSize = 100 };
    return new MemoryCacheCore(cacheConfig, provider.GetService<ILogger<MemoryCacheCore>>());
});
```

Then, you can inject IGenericCache into your services and use it for caching data:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

public class MyController : ControllerBase
{
    private readonly IGenericCache _cache;
    private readonly ILogger<MyController> _logger;

    public MyController(IGenericCache cache, ILogger<MyController> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<IActionResult> GetData(string key)
    {
        if (_cache.TryGetAsync<string>(key, out var value))
        {
            _logger.LogInformation($"Data found in cache for key: {key}");
            return Ok(value);
        }

        // Fetch data from database or other sources
        var data = await GetDataFromDatabaseAsync(key);

        // Cache the data
        _cache.SetAsync(key, data).Wait();

        return Ok(data);
    }

    // Other action methods and class logic go here
}
```

We also recommend to check the [example](https://github.com/PeterKottas/generic-cache/blob/main/Source/Finbourne.GenericCache.Example/Program.cs)

## Contributing
Contributions are welcome! Especially if these are contributions from Peter Kottas to Finbourne as a result of employment.

## License
This project is licensed under the MIT License.
