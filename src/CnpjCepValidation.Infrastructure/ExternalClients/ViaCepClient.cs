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

public sealed class ViaCepClient : ICepAddressProvider
{
    public const string Provider = "ViaCep";

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
        try
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
                "ViaCEP",
                body.Uf,
                body.Localidade,
                body.Logradouro);
        }
        catch (DependencyUnavailableException)
        {
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "ViaCEP: payload invalido");
            throw new DependencyUnavailableException("ViaCEP indisponivel (payload invalido).", ex);
        }
        catch (NotSupportedException ex)
        {
            _logger.LogWarning(ex, "ViaCEP: payload nao suportado");
            throw new DependencyUnavailableException("ViaCEP indisponivel (payload invalido).", ex);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogWarning(ex, "ViaCEP: circuit breaker aberto");
            throw new DependencyUnavailableException("ViaCEP indisponivel (circuit breaker).", ex);
        }
        catch (TimeoutRejectedException ex)
        {
            _logger.LogWarning(ex, "ViaCEP: timeout");
            throw new DependencyUnavailableException("ViaCEP indisponivel (timeout).", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "ViaCEP: falha de rede");
            throw new DependencyUnavailableException("ViaCEP indisponivel (falha de rede).", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "ViaCEP: timeout (cancelamento interno)");
            throw new DependencyUnavailableException("ViaCEP indisponivel (timeout).", ex);
        }
    }
}
