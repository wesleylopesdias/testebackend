using CnpjCepValidation.Application.Exceptions;
using CnpjCepValidation.Application.Models;

namespace CnpjCepValidation.Infrastructure.ExternalClients;

internal static class ExternalAddressPayloadValidator
{
    public static CompanyInfo CreateCompanyInfo(
        string providerName,
        string? state,
        string? city,
        string? street)
    {
        return new CompanyInfo(
            State: RequireComponent(providerName, "uf", state),
            City: RequireComponent(providerName, "cidade", city),
            Street: RequireComponent(providerName, "logradouro", street));
    }

    public static CepAddressInfo CreateCepAddress(
        string providerName,
        string? state,
        string? city,
        string? street)
    {
        return new CepAddressInfo(
            State: RequireComponent(providerName, "uf", state),
            City: RequireComponent(providerName, "cidade", city),
            Street: RequireComponent(providerName, "logradouro", street));
    }

    private static string RequireComponent(string providerName, string componentName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DependencyUnavailableException(
                $"{providerName} retornou payload de endereco incompleto: {componentName}.");
        }

        return value.Trim();
    }
}
