using CnpjCepValidation.Application.Abstractions;
using CnpjCepValidation.Infrastructure.ExternalClients;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CnpjCepValidation.Integration.Fixtures;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly IReadOnlyDictionary<string, string?> _configurationOverrides;

    public FakeHttpMessageHandler CnpjHandler { get; } = new();
    public FakeHttpMessageHandler BrasilApiCepHandler { get; } = new();
    public FakeHttpMessageHandler ViaCepHandler { get; } = new();

    public CustomWebApplicationFactory(IDictionary<string, string?>? configurationOverrides = null)
    {
        var defaults = new Dictionary<string, string?>
        {
            ["Validation:CnpjRetryCount"] = "0",
            ["Validation:CepPrimaryRetryCount"] = "0",
            ["Validation:CepSecondaryRetryCount"] = "0",
            ["Validation:TimeoutMs"] = "5000",
            ["Validation:CacheTtlMinutes"] = "0",
            ["Validation:NegativeCacheTtlMinutes"] = "0"
        };

        if (configurationOverrides is not null)
        {
            foreach (var (key, value) in configurationOverrides)
            {
                defaults[key] = value;
            }
        }

        _configurationOverrides = defaults;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(config =>
            config.AddInMemoryCollection(_configurationOverrides));

        builder.ConfigureServices(services =>
        {
            var cacheDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IMemoryCache));
            if (cacheDescriptor is not null) services.Remove(cacheDescriptor);
            services.AddSingleton<IMemoryCache, NoOpMemoryCache>();

            var validationCacheDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IValidationCache));
            if (validationCacheDescriptor is not null) services.Remove(validationCacheDescriptor);
            services.AddSingleton<IValidationCache, NoOpValidationCache>();

            services.AddHttpClient<BrasilApiCnpjClient>()
                .ConfigurePrimaryHttpMessageHandler(() => CnpjHandler);

            services.AddHttpClient<BrasilApiCepClient>()
                .ConfigurePrimaryHttpMessageHandler(() => BrasilApiCepHandler);

            services.AddHttpClient<ViaCepClient>()
                .ConfigurePrimaryHttpMessageHandler(() => ViaCepHandler);
        });

        builder.UseEnvironment("Testing");
    }
}
