using System.Net;
using System.Net.Http.Json;
using System.Diagnostics;
using System.Threading;
using CnpjCepValidation.Performance.Fixtures;
using FluentAssertions;
using NBomber.CSharp;

namespace CnpjCepValidation.Performance;

public sealed class ValidationPerformanceSmokeTests
{
    private const string Endpoint = "/api/v1/customer-registration-validations";
    private const string ValidCnpj = "00924432000199";
    private const string ValidCep = "13288190";

    [Fact]
    public async Task HotCache_LoadSmoke_ShouldStayHealthy_AndMinimizeExternalCalls()
    {
        using var factory = new PerformanceWebApplicationFactory();
        using var client = factory.CreateClient();
        var stopwatch = Stopwatch.StartNew();
        var failureCount = 0;

        factory.CnpjHandler.When("cnpj/v1/", HttpStatusCode.OK, """
            {
                "uf": "SP",
                "municipio": "VINHEDO",
                "logradouro": "DA FAZENDA PAU A PIQUE",
                "descricao_tipo_de_logradouro": "ESTRADA"
            }
            """);

        factory.BrasilApiCepHandler.When("cep/v2/", HttpStatusCode.OK, """
            {
                "state": "SP",
                "city": "VINHEDO",
                "street": "ESTRADA DA FAZENDA PAU A PIQUE"
            }
            """);

        var warmUpResponse = await client.PostAsJsonAsync(Endpoint, new { cnpj = ValidCnpj, cep = ValidCep });
        warmUpResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var scenario = Scenario.Create("validation_api_hot_cache", async context =>
        {
            return await Step.Run("post_validation", context, async () =>
            {
                using var response = await client.PostAsJsonAsync(
                    Endpoint,
                    new { cnpj = ValidCnpj, cep = ValidCep });

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return Response.Ok(statusCode: "200");
                }

                Interlocked.Increment(ref failureCount);
                return Response.Fail(statusCode: ((int)response.StatusCode).ToString());
            });
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 30, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(2)));

        NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder(Path.Combine(Path.GetTempPath(), "CnpjCepValidation.NBomber"))
            .Run();

        stopwatch.Stop();

        failureCount.Should().Be(0);
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(10));
        factory.CnpjHandler.CallCount.Should().BeLessThan(10);
        factory.BrasilApiCepHandler.CallCount.Should().BeLessThan(10);
        factory.ViaCepHandler.CallCount.Should().Be(0);
    }
}
