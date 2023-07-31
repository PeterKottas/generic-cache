using Finbourne.GenericCache.Core.Interface;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finbourne.GenericCache.Memory
{
    public static class MemoryCacheExtensions
    {
        public static IServiceCollection AddMemoryCache(this IServiceCollection services, MemoryCacheConfig config)
        {
            services.AddSingleton(config);
            services.AddSingleton<IGenericCache, MemoryCacheCore>();
            return services;
        }
    }
}
