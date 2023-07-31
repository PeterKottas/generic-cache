using Finbourne.GenericCache.Core.Model.Enum;

namespace Finbourne.GenericCache.Core.Interface
{
    public interface IGenericCache
    {
        public Task<bool> TryGetAsync<T>(string key, out T value);

        public Task SetAsync<T>(string key, T value);

        public Task<bool> DeleteAsync<T>(string key);

        public Task PurgeAsync();

        public Task<string> SubscribeDeleteAsync<T>(Action<string, CacheDeletionReasonEnum, T> action);

        public Task<bool> UnSubscribeDeleteAsync<T>(string subscription);
    }
}