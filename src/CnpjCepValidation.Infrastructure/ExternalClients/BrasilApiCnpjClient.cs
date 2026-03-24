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

public sealed class BrasilApiCnpjClient : ICompanyRegistryClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BrasilApiCnpjClient> _logger;

    public BrasilApiCnpjClient(HttpClient httpClient, ILogger<BrasilApiCnpjClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CompanyInfo?> GetCompanyAsync(Cnpj cnpj, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"/api/cnpj/v1/{cnpj.Value}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogInformation("BrasilAPI CNPJ: empresa não encontrada para {Cnpj}", cnpj.Value);
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "BrasilAPI CNPJ retornou status {Status} para {Cnpj}",
                    (int)response.StatusCode, cnpj.Value);
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
        }
        catch (DependencyUnavailableException)
        {
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "BrasilAPI CNPJ: payload invalido");
            throw new DependencyUnavailableException("BrasilAPI CNPJ indisponivel (payload invalido).", ex);
        }
        catch (NotSupportedException ex)
        {
            _logger.LogWarning(ex, "BrasilAPI CNPJ: payload nao suportado");
            throw new DependencyUnavailableException("BrasilAPI CNPJ indisponivel (payload invalido).", ex);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogWarning(ex, "BrasilAPI CNPJ: circuit breaker aberto");
            throw new DependencyUnavailableException("BrasilAPI CNPJ indisponível (circuit breaker).", ex);
        }
        catch (TimeoutRejectedException ex)
        {
            _logger.LogWarning(ex, "BrasilAPI CNPJ: timeout");
            throw new DependencyUnavailableException("BrasilAPI CNPJ indisponível (timeout).", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "BrasilAPI CNPJ: falha de rede");
            throw new DependencyUnavailableException("BrasilAPI CNPJ indisponível (falha de rede).", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "BrasilAPI CNPJ: timeout (cancelamento interno)");
            throw new DependencyUnavailableException("BrasilAPI CNPJ indisponível (timeout).", ex);
        }
    }

    private static CompanyInfo MapToCompanyInfo(BrasilApiCnpjResponse response)
    {
        var street = BuildStreet(response.DescricaoTipoDeLogradouro, response.Logradouro);
        return ExternalAddressPayloadValidator.CreateCompanyInfo(
            "BrasilAPI CNPJ",
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
