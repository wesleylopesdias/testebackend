using CnpjCepValidation.Application.Models;
using CnpjCepValidation.Domain.ValueObjects;

namespace CnpjCepValidation.Application.Abstractions;

public interface ICepAddressResolver
{
    Task<CepResolveResult> ResolveAsync(Cep cep, CancellationToken cancellationToken);
}
