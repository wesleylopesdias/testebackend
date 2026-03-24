using System.Net;
using System.Net.Http.Json;
using CnpjCepValidation.Application.Abstractions;
using CnpjCepValidation.Application.Exceptions;
using CnpjCepValidation.Application.Models;
using CnpjCepValidation.Domain.ValueObjects;
using CnpjCepValidation.Infrastructure.ExternalClients.Models;
using Microsoft.Extensions.Logging;

namespace CnpjCepValidation.Infrastructure.ExternalClients;

public sealed class ViaCepClient : ICepAddressProvider
{
    public const string Provider = "ViaCep";
    private const string ProviderLabel = "ViaCEP";

    private readonly HttpClient _httpClient;
    private readonly ILogger<ViaCepClient> _logger;

    public string ProviderName => Provider;

    public ViaCepClient(HttpClient httpClient, ILogger<ViaCepClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CepAddressInfo?> GetAddressAsync(Cep cep, CancellationToken cancellationToken)
    {
        return await ResilientHttpExecutor.ExecuteAsync(ProviderLabel, _logger, async () =>
        {
            var response = await _httpClient.GetAsync(
                $"/ws/{cep.Value}/json/", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogInformation("ViaCEP: endereco nao encontrado para {Cep}", cep.Value);
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "ViaCEP retornou status {Status} para {Cep}",
                    (int)response.StatusCode, cep.Value);
                throw new DependencyUnavailableException(
                    $"ViaCEP retornou status {(int)response.StatusCode}.");
            }

            var body = await response.Content.ReadFromJsonAsync<ViaCepResponse>(
                cancellationToken: cancellationToken);

            if (body?.Erro == true)
            {
                return null;
            }

            if (body is null)
            {
                throw new DependencyUnavailableException("ViaCEP retornou payload vazio.");
            }

            return ExternalAddressPayloadValidator.CreateCepAddress(
                ProviderLabel,
                body.Uf,
                body.Localidade,
                body.Logradouro);
        }, cancellationToken);
    }
}
