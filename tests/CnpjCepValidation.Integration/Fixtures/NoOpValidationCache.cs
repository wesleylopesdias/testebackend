using CnpjCepValidation.Application.Abstractions;

namespace CnpjCepValidation.Integration.Fixtures;

internal sealed class NoOpValidationCache : IValidationCache
{
    public bool TryGet<T>(string key, out T? value)
    {
        value = default;
        return false;
    }

    public void Set<T>(string key, T value, TimeSpan ttl) { }
}
