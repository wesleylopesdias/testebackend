using CnpjCepValidation.Application.Abstractions;
using Microsoft.Extensions.Caching.Memory;

namespace CnpjCepValidation.Infrastructure.Services;

public sealed class MemoryValidationCache : IValidationCache
{
    private readonly IMemoryCache _cache;

    public MemoryValidationCache(IMemoryCache cache) => _cache = cache;

    public bool TryGet<T>(string key, out T? value) =>
        _cache.TryGetValue(key, out value);

    public void Set<T>(string key, T value, TimeSpan ttl) =>
        _cache.Set(key, value, ttl);
}
