using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CnpjCepValidation.Application.Abstractions;
using CnpjCepValidation.Application.Exceptions;
using CnpjCepValidation.Application.Models;
using CnpjCepValidation.Domain.ValueObjects;
using CnpjCepValidation.Infrastructure.ExternalClients.Models;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace CnpjCepValidation.Infrastructure.ExternalClients;

public sealed class BrasilApiCepClient : ICepAddressProvider
{
    public const string Provider = "BrasilApi";

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
        try
        {
            var response = await _httpClient.GetAsync(
                $"/api/cep/v2/{cep.Value}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogInformation("BrasilAPI CEP: endereço não encontrado para {Cep}", cep.Value);
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
                "BrasilAPI CEP",
                body.State,
                body.City,
                body.Street);
        }
        catch (DependencyUnavailableException)
        {
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "BrasilAPI CEP: payload invalido");
            throw new DependencyUnavailableException("BrasilAPI CEP indisponivel (payload invalido).", ex);
        }
        catch (NotSupportedException ex)
        {
            _logger.LogWarning(ex, "BrasilAPI CEP: payload nao suportado");
            throw new DependencyUnavailableException("BrasilAPI CEP indisponivel (payload invalido).", ex);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogWarning(ex, "BrasilAPI CEP: circuit breaker aberto");
            throw new DependencyUnavailableException("BrasilAPI CEP indisponível (circuit breaker).", ex);
        }
        catch (TimeoutRejectedException ex)
        {
            _logger.LogWarning(ex, "BrasilAPI CEP: timeout");
            throw new DependencyUnavailableException("BrasilAPI CEP indisponível (timeout).", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "BrasilAPI CEP: falha de rede");
            throw new DependencyUnavailableException("BrasilAPI CEP indisponível (falha de rede).", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "BrasilAPI CEP: timeout (cancelamento interno)");
            throw new DependencyUnavailableException("BrasilAPI CEP indisponível (timeout).", ex);
        }
    }
}
