using CnpjCepValidation.Infrastructure.ExternalClients;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CnpjCepValidation.Performance.Fixtures;

internal sealed class PerformanceWebApplicationFactory : WebApplicationFactory<Program>
{
    public FakeHttpMessageHandler CnpjHandler { get; } = new();
    public FakeHttpMessageHandler BrasilApiCepHandler { get; } = new();
    public FakeHttpMessageHandler ViaCepHandler { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(config =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Validation:PrimaryCepProvider"] = "BrasilApi",
                ["Validation:CnpjRetryCount"] = "0",
                ["Validation:CepPrimaryRetryCount"] = "0",
                ["Validation:CepSecondaryRetryCount"] = "0",
                ["Validation:TimeoutMs"] = "5000",
                ["Validation:CacheTtlMinutes"] = "5"
            }));

        builder.ConfigureServices(services =>
        {
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
