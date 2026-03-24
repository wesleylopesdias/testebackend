namespace CnpjCepValidation.Application.Abstractions;

public interface IValidationCache
{
    bool TryGet<T>(string key, out T? value);
    void Set<T>(string key, T value, TimeSpan ttl);
}
