using CnpjCepValidation.Application.Models;
using CnpjCepValidation.Domain.ValueObjects;

namespace CnpjCepValidation.Application.Abstractions;

public interface ICompanyRegistryClient
{
    Task<CompanyInfo?> GetCompanyAsync(Cnpj cnpj, CancellationToken cancellationToken);
}
