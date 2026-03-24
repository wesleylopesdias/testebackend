using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace CnpjCepValidation.Integration.Fixtures;

internal sealed class NoOpMemoryCache : IMemoryCache
{
    public ICacheEntry CreateEntry(object key) => new NoOpCacheEntry(key);
    public void Remove(object key) { }
    public bool TryGetValue(object key, out object? value) { value = null; return false; }
    public void Dispose() { }

    private sealed class NoOpCacheEntry : ICacheEntry
    {
        public NoOpCacheEntry(object key) => Key = key;
        public object Key { get; }
        public object? Value { get; set; }
        public DateTimeOffset? AbsoluteExpiration { get; set; }
        public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }
        public TimeSpan? SlidingExpiration { get; set; }
        public IList<IChangeToken> ExpirationTokens { get; } = [];
        public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks { get; } = [];
        public CacheItemPriority Priority { get; set; }
        public long? Size { get; set; }
        public void Dispose() { }
    }
}
