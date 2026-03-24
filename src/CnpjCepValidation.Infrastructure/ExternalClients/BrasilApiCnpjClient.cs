using System.Net;
using System.Net.Http.Json;
using CnpjCepValidation.Application.Abstractions;
using CnpjCepValidation.Application.Exceptions;
using CnpjCepValidation.Application.Models;
using CnpjCepValidation.Domain.ValueObjects;
using CnpjCepValidation.Infrastructure.ExternalClients.Models;
using Microsoft.Extensions.Logging;

namespace CnpjCepValidation.Infrastructure.ExternalClients;

public sealed class BrasilApiCnpjClient : ICompanyRegistryClient
{
    private const string ProviderName = "BrasilAPI CNPJ";

    private readonly HttpClient _httpClient;
    private readonly ILogger<BrasilApiCnpjClient> _logger;

    public BrasilApiCnpjClient(HttpClient httpClient, ILogger<BrasilApiCnpjClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CompanyInfo?> GetCompanyAsync(Cnpj cnpj, CancellationToken cancellationToken)
    {
        return await ResilientHttpExecutor.ExecuteAsync(ProviderName, _logger, async () =>
        {
            var response = await _httpClient.GetAsync(
                $"/api/cnpj/v1/{cnpj.Value}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogInformation("BrasilAPI CNPJ: empresa nao encontrada para {CnpjMasked}", CnpjMask.Apply(cnpj.Value));
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "BrasilAPI CNPJ retornou status {Status} para {CnpjMasked}",
                    (int)response.StatusCode, CnpjMask.Apply(cnpj.Value));
                throw new DependencyUnavailableException(
                    $"BrasilAPI CNPJ retornou status {(int)response.StatusCode}.");
            }

            var body = await response.Content.ReadFromJsonAsync<BrasilApiCnpjResponse>(
                cancellationToken: cancellationToken);

            if (body is null)
            {
                throw new DependencyUnavailableException("BrasilAPI CNPJ retornou payload vazio.");
            }

            return MapToCompanyInfo(body);
        }, cancellationToken);
    }

    private static CompanyInfo MapToCompanyInfo(BrasilApiCnpjResponse response)
    {
        var street = BuildStreet(response.DescricaoTipoDeLogradouro, response.Logradouro);
        return ExternalAddressPayloadValidator.CreateCompanyInfo(
            ProviderName,
            response.Uf,
            response.Municipio,
            street);
    }

    private static string BuildStreet(string? tipo, string? logradouro)
    {
        if (string.IsNullOrWhiteSpace(logradouro)) return string.Empty;
        if (string.IsNullOrWhiteSpace(tipo)) return logradouro;
        return $"{tipo} {logradouro}";
    }
}
