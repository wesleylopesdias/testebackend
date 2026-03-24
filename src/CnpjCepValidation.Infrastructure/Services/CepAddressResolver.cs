using CnpjCepValidation.Application.Abstractions;
using CnpjCepValidation.Application.Diagnostics;
using CnpjCepValidation.Application.Exceptions;
using CnpjCepValidation.Application.Models;
using CnpjCepValidation.Application.Options;
using CnpjCepValidation.Domain.ValueObjects;
using CnpjCepValidation.Infrastructure.ExternalClients;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CnpjCepValidation.Infrastructure.Services;

public sealed class CepAddressResolver : ICepAddressResolver
{
    private readonly Func<(ICepAddressProvider Primary, ICepAddressProvider Secondary)> _providerSelector;
    private readonly ILogger<CepAddressResolver> _logger;

    public CepAddressResolver(
        ICepAddressProvider primary,
        ICepAddressProvider secondary,
        ILogger<CepAddressResolver> logger)
    {
        _providerSelector = () => (primary, secondary);
        _logger = logger;
    }

    public CepAddressResolver(
        BrasilApiCepClient brasilApiProvider,
        ViaCepClient viaCepProvider,
        IOptions<ValidationOptions> options,
        ILogger<CepAddressResolver> logger)
    {
        var configuredPrimary = options.Value.PrimaryCepProvider;
        _providerSelector = () => ResolveProviders(
            brasilApiProvider,
            viaCepProvider,
            configuredPrimary);
        _logger = logger;
    }

    public async Task<CepResolveResult> ResolveAsync(Cep cep, CancellationToken cancellationToken)
    {
        var (primary, secondary) = _providerSelector();

        try
        {
            var address = await primary.GetAddressAsync(cep, cancellationToken);
            return new CepResolveResult(address, primary.ProviderName);
        }
        catch (DependencyUnavailableException ex)
        {
            ValidationDiagnostics.RecordCepFallback(
                primary.ProviderName,
                secondary.ProviderName,
                ex.GetType().Name);

            _logger.LogWarning(
                ex,
                "Primary CEP provider ({Provider}) failed. Triggering fallback.",
                primary.ProviderName);
        }

        var fallbackAddress = await secondary.GetAddressAsync(cep, cancellationToken);
        return new CepResolveResult(fallbackAddress, secondary.ProviderName);
    }

    private static (ICepAddressProvider Primary, ICepAddressProvider Secondary) ResolveProviders(
        BrasilApiCepClient brasilApiProvider,
        ViaCepClient viaCepProvider,
        string primaryCepProvider)
    {
        if (string.Equals(primaryCepProvider, ViaCepClient.Provider, StringComparison.OrdinalIgnoreCase))
        {
            return (viaCepProvider, brasilApiProvider);
        }

        if (string.Equals(primaryCepProvider, BrasilApiCepClient.Provider, StringComparison.OrdinalIgnoreCase))
        {
            return (brasilApiProvider, viaCepProvider);
        }

        throw new InvalidOperationException(
            $"Unsupported CEP provider configured as primary: {primaryCepProvider}.");
    }
}
