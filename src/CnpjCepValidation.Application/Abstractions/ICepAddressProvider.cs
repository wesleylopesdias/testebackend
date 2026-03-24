using CnpjCepValidation.Application.Models;
using CnpjCepValidation.Domain.ValueObjects;

namespace CnpjCepValidation.Application.Abstractions;

public interface ICepAddressProvider
{
    string ProviderName { get; }
    Task<CepAddressInfo?> GetAddressAsync(Cep cep, CancellationToken cancellationToken);
}
