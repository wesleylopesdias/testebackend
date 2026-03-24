using System.Net;
using System.Net.Http.Json;
using CnpjCepValidation.Application.Abstractions;
using CnpjCepValidation.Application.Exceptions;
using CnpjCepValidation.Application.Models;
using CnpjCepValidation.Domain.ValueObjects;
using CnpjCepValidation.Infrastructure.ExternalClients.Models;
using Microsoft.Extensions.Logging;

namespace CnpjCepValidation.Infrastructure.ExternalClients;

public sealed class BrasilApiCepClient : ICepAddressProvider
{
    public const string Provider = "BrasilApi";
    private const string ProviderLabel = "BrasilAPI CEP";

    private readonly HttpClient _httpClient;
    private readonly ILogger<BrasilApiCepClient> _logger;

    public string ProviderName => Provider;

    public BrasilApiCepClient(HttpClient httpClient, ILogger<BrasilApiCepClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CepAddressInfo?> GetAddressAsync(Cep cep, CancellationToken cancellationToken)
    {
        return await ResilientHttpExecutor.ExecuteAsync(ProviderLabel, _logger, async () =>
        {
            var response = await _httpClient.GetAsync(
                $"/api/cep/v2/{cep.Value}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogInformation("BrasilAPI CEP: endereco nao encontrado para {Cep}", cep.Value);
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "BrasilAPI CEP retornou status {Status} para {Cep}",
                    (int)response.StatusCode, cep.Value);
                throw new DependencyUnavailableException(
                    $"BrasilAPI CEP retornou status {(int)response.StatusCode}.");
            }

            var body = await response.Content.ReadFromJsonAsync<BrasilApiCepResponse>(
                cancellationToken: cancellationToken);

            if (body is null)
            {
                throw new DependencyUnavailableException("BrasilAPI CEP retornou payload vazio.");
            }

            return ExternalAddressPayloadValidator.CreateCepAddress(
                ProviderLabel,
                body.State,
                body.City,
                body.Street);
        }, cancellationToken);
    }
}
