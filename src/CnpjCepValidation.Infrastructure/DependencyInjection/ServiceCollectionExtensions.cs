using System.Net;
using CnpjCepValidation.Application.Abstractions;
using CnpjCepValidation.Application.Diagnostics;
using CnpjCepValidation.Application.Options;
using CnpjCepValidation.Application.Services;
using CnpjCepValidation.Application.UseCases;
using CnpjCepValidation.Infrastructure.ExternalClients;
using CnpjCepValidation.Infrastructure.Options;
using CnpjCepValidation.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.Timeout;

namespace CnpjCepValidation.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<ValidationOptions>()
            .Bind(configuration.GetSection(ValidationOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<ExternalApiOptions>()
            .Bind(configuration.GetSection(ExternalApiOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var validationOptions = configuration
            .GetSection(ValidationOptions.SectionName)
            .Get<ValidationOptions>() ?? new ValidationOptions();

        var externalApiOptions = configuration
            .GetSection(ExternalApiOptions.SectionName)
            .Get<ExternalApiOptions>() ?? new ExternalApiOptions();

        services.AddMemoryCache();
        services.AddSingleton<IValidationCache, MemoryValidationCache>();

        RegisterCnpjClient(services, externalApiOptions, validationOptions);
        RegisterCepClients(services, externalApiOptions, validationOptions);

        services.AddSingleton<IAddressComparer, AddressComparer>();
        services.AddScoped<IRegistrationValidationUseCase, ValidateCustomerRegistrationUseCase>();

        return services;
    }

    private static void RegisterCnpjClient(
        IServiceCollection services,
        ExternalApiOptions apiOptions,
        ValidationOptions validationOptions)
    {
        services.AddHttpClient<BrasilApiCnpjClient>(client =>
            client.BaseAddress = new Uri(apiOptions.BrasilApiBaseUrl))
            .AddResilienceHandler("cnpj-pipeline", builder =>
            {
                builder.AddTimeout(TimeSpan.FromMilliseconds(validationOptions.TimeoutMs));
                builder.AddRetry(BuildRetryOptions("brasilapi_cnpj", validationOptions.CnpjRetryCount));
                builder.AddCircuitBreaker(BuildCircuitBreakerOptions());
            });

        services.AddTransient<ICompanyRegistryClient>(sp =>
            sp.GetRequiredService<BrasilApiCnpjClient>());
    }

    private static void RegisterCepClients(
        IServiceCollection services,
        ExternalApiOptions apiOptions,
        ValidationOptions validationOptions)
    {
        services.AddHttpClient<BrasilApiCepClient>(client =>
            client.BaseAddress = new Uri(apiOptions.BrasilApiBaseUrl))
            .AddResilienceHandler("cep-primary-pipeline", builder =>
            {
                builder.AddTimeout(TimeSpan.FromMilliseconds(validationOptions.TimeoutMs));
                builder.AddRetry(BuildRetryOptions("brasilapi_cep", validationOptions.CepPrimaryRetryCount));
                builder.AddCircuitBreaker(BuildCircuitBreakerOptions());
            });

        services.AddHttpClient<ViaCepClient>(client =>
            client.BaseAddress = new Uri(apiOptions.ViaCepBaseUrl))
            .AddResilienceHandler("cep-secondary-pipeline", builder =>
            {
                builder.AddTimeout(TimeSpan.FromMilliseconds(validationOptions.TimeoutMs));
                builder.AddRetry(BuildRetryOptions("viacep", validationOptions.CepSecondaryRetryCount));
                builder.AddCircuitBreaker(BuildCircuitBreakerOptions());
            });

        services.AddTransient<ICepAddressResolver, CepAddressResolver>();
    }

    private static HttpRetryStrategyOptions BuildRetryOptions(string dependency, int maxRetries) =>
        new()
        {
            MaxRetryAttempts = maxRetries,
            Delay = TimeSpan.FromMilliseconds(150),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            OnRetry = args =>
            {
                ValidationDiagnostics.RecordRetry(dependency, args.AttemptNumber + 1);
                return default;
            },
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .Handle<TimeoutRejectedException>()
                .Handle<HttpRequestException>()
                .HandleResult(r => IsTransient(r.StatusCode))
        };

    private static HttpCircuitBreakerStrategyOptions BuildCircuitBreakerOptions() =>
        new()
        {
            SamplingDuration = TimeSpan.FromSeconds(30),
            FailureRatio = 0.5,
            MinimumThroughput = 5,
            BreakDuration = TimeSpan.FromSeconds(15),
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .Handle<TimeoutRejectedException>()
                .Handle<HttpRequestException>()
                .HandleResult(r => IsTransient(r.StatusCode))
        };

    private static bool IsTransient(HttpStatusCode statusCode) =>
        (int)statusCode >= 500 ||
        statusCode == HttpStatusCode.RequestTimeout ||
        statusCode == HttpStatusCode.TooManyRequests;
}
