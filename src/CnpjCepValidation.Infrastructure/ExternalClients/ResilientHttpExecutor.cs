using CnpjCepValidation.Application.Exceptions;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace CnpjCepValidation.Infrastructure.ExternalClients;

internal static class ResilientHttpExecutor
{
    public static async Task<T> ExecuteAsync<T>(
        string providerName,
        ILogger logger,
        Func<Task<T>> action,
        CancellationToken cancellationToken)
    {
        try
        {
            return await action();
        }
        catch (DependencyUnavailableException)
        {
            throw;
        }
        catch (System.Text.Json.JsonException ex)
        {
            logger.LogWarning(ex, "{Provider}: payload invalido", providerName);
            throw new DependencyUnavailableException($"{providerName} indisponivel (payload invalido).", ex);
        }
        catch (NotSupportedException ex)
        {
            logger.LogWarning(ex, "{Provider}: payload nao suportado", providerName);
            throw new DependencyUnavailableException($"{providerName} indisponivel (payload invalido).", ex);
        }
        catch (BrokenCircuitException ex)
        {
            logger.LogWarning(ex, "{Provider}: circuit breaker aberto", providerName);
            throw new DependencyUnavailableException($"{providerName} indisponivel (circuit breaker).", ex);
        }
        catch (TimeoutRejectedException ex)
        {
            logger.LogWarning(ex, "{Provider}: timeout", providerName);
            throw new DependencyUnavailableException($"{providerName} indisponivel (timeout).", ex);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "{Provider}: falha de rede", providerName);
            throw new DependencyUnavailableException($"{providerName} indisponivel (falha de rede).", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "{Provider}: timeout (cancelamento interno)", providerName);
            throw new DependencyUnavailableException($"{providerName} indisponivel (timeout).", ex);
        }
    }
}
