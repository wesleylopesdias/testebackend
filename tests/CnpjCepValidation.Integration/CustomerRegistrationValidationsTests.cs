using System.Net;
using System.Net.Http.Json;
using CnpjCepValidation.Application.DTOs;
using CnpjCepValidation.Integration.Fixtures;
using FluentAssertions;

namespace CnpjCepValidation.Integration;

[Collection("Integration")]
public sealed class CustomerRegistrationValidationsTests : IDisposable
{
    private const string ValidCnpj = "00924432000199";
    private const string ValidCep = "13288390";
    private const string Endpoint = "/api/v1/customer-registration-validations";

    private const string CnpjMatchJson = """
        {
            "uf": "SP",
            "municipio": "VINHEDO",
            "logradouro": "MUNICIPAL VINHEDO/ITUPEVA",
            "descricao_tipo_de_logradouro": "ESTRADA"
        }
        """;

    private const string CepMatchJson = """
        {
            "state": "SP",
            "city": "VINHEDO",
            "street": "ESTRADA MUNICIPAL VINHEDO/ITUPEVA"
        }
        """;

    private const string CepMismatchJson = """
        {
            "state": "SP",
            "city": "Vinhedo",
            "street": "Rodovia dos Bandeirantes"
        }
        """;

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CustomerRegistrationValidationsTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task Post_MatchingAddresses_Returns200WithMatchReason()
    {
        _factory.CnpjHandler.When("cnpj/v1/", HttpStatusCode.OK, CnpjMatchJson);
        _factory.BrasilApiCepHandler.When("cep/v2/", HttpStatusCode.OK, CepMatchJson);

        var response = await _client.PostAsJsonAsync(Endpoint, new { cnpj = ValidCnpj, cep = ValidCep });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ValidateCustomerRegistrationResponse>();
        body!.IsMatch.Should().BeTrue();
        body.Reason.Should().Be(ValidationReason.Match);
        body.CepProvider.Should().Be("BrasilApi");
    }

    [Fact]
    public async Task Post_MismatchingAddresses_Returns404WithMismatchReason()
    {
        _factory.CnpjHandler.When("cnpj/v1/", HttpStatusCode.OK, CnpjMatchJson);
        _factory.BrasilApiCepHandler.When("cep/v2/", HttpStatusCode.OK, CepMismatchJson);

        var response = await _client.PostAsJsonAsync(Endpoint, new { cnpj = ValidCnpj, cep = ValidCep });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<ValidateCustomerRegistrationResponse>();
        body!.IsMatch.Should().BeFalse();
        body.Reason.Should().Be(ValidationReason.AddressMismatch);
    }

    [Fact]
    public async Task Post_InvalidCnpj_Returns400()
    {
        var response = await _client.PostAsJsonAsync(Endpoint, new { cnpj = "00000000000000", cep = ValidCep });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_InvalidCep_Returns400()
    {
        var response = await _client.PostAsJsonAsync(Endpoint, new { cnpj = ValidCnpj, cep = "123" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_CnpjNotFound_Returns404WithCompanyNotFoundReason()
    {
        _factory.CnpjHandler.When("cnpj/v1/", HttpStatusCode.NotFound);

        var response = await _client.PostAsJsonAsync(Endpoint, new { cnpj = ValidCnpj, cep = ValidCep });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<ValidateCustomerRegistrationResponse>();
        body!.Reason.Should().Be(ValidationReason.CompanyNotFound);
    }

    [Fact]
    public async Task Post_CepNotFound_Returns404WithPostalCodeNotFoundReason()
    {
        _factory.CnpjHandler.When("cnpj/v1/", HttpStatusCode.OK, CnpjMatchJson);
        _factory.BrasilApiCepHandler.When("cep/v2/", HttpStatusCode.NotFound);

        var response = await _client.PostAsJsonAsync(Endpoint, new { cnpj = ValidCnpj, cep = ValidCep });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<ValidateCustomerRegistrationResponse>();
        body!.Reason.Should().Be(ValidationReason.PostalCodeNotFound);
    }

    [Fact]
    public async Task Post_AllDependenciesUnavailable_Returns503WithDependencyUnavailableReason()
    {
        _factory.CnpjHandler.When("cnpj/v1/", HttpStatusCode.InternalServerError);

        var response = await _client.PostAsJsonAsync(Endpoint, new { cnpj = ValidCnpj, cep = ValidCep });

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var body = await response.Content.ReadFromJsonAsync<ValidateCustomerRegistrationResponse>();
        body!.Reason.Should().Be(ValidationReason.DependencyUnavailable);
    }

    [Fact]
    public async Task Post_PrimaryCepFails_FallsBackToViaCep_Returns200()
    {
        _factory.CnpjHandler.When("cnpj/v1/", HttpStatusCode.OK, CnpjMatchJson);
        _factory.BrasilApiCepHandler.When("cep/v2/", HttpStatusCode.InternalServerError);
        _factory.ViaCepHandler.When("/ws/", HttpStatusCode.OK, """
            {
                "uf": "SP",
                "localidade": "VINHEDO",
                "logradouro": "ESTRADA MUNICIPAL VINHEDO/ITUPEVA"
            }
            """);

        var response = await _client.PostAsJsonAsync(Endpoint, new { cnpj = ValidCnpj, cep = ValidCep });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ValidateCustomerRegistrationResponse>();
        body!.IsMatch.Should().BeTrue();
        body.CepProvider.Should().Be("ViaCep");
    }

    [Fact]
    public async Task Post_CnpjProviderReturnsIncompletePayload_Returns503WithDependencyUnavailableReason()
    {
        _factory.CnpjHandler.When("cnpj/v1/", HttpStatusCode.OK, """
            {
                "uf": "SP",
                "municipio": "VINHEDO"
            }
            """);

        var response = await _client.PostAsJsonAsync(Endpoint, new { cnpj = ValidCnpj, cep = ValidCep });

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var body = await response.Content.ReadFromJsonAsync<ValidateCustomerRegistrationResponse>();
        body!.Reason.Should().Be(ValidationReason.DependencyUnavailable);
    }

    [Fact]
    public async Task Post_PrimaryCepMalformedJson_FallsBackToViaCep_Returns200()
    {
        _factory.CnpjHandler.When("cnpj/v1/", HttpStatusCode.OK, CnpjMatchJson);
        _factory.BrasilApiCepHandler.When("cep/v2/", HttpStatusCode.OK, "{ invalid json");
        _factory.ViaCepHandler.When("/ws/", HttpStatusCode.OK, """
            {
                "uf": "SP",
                "localidade": "VINHEDO",
                "logradouro": "ESTRADA MUNICIPAL VINHEDO/ITUPEVA"
            }
            """);

        var response = await _client.PostAsJsonAsync(Endpoint, new { cnpj = ValidCnpj, cep = ValidCep });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ValidateCustomerRegistrationResponse>();
        body!.IsMatch.Should().BeTrue();
        body.CepProvider.Should().Be("ViaCep");
    }

    [Fact]
    public async Task Post_PrimaryCepRetriesBeforeFallback_UsesConfiguredAttemptCount()
    {
        using var factory = new CustomWebApplicationFactory(new Dictionary<string, string?>
        {
            ["Validation:CnpjRetryCount"] = "0",
            ["Validation:CepPrimaryRetryCount"] = "2",
            ["Validation:CepSecondaryRetryCount"] = "0",
            ["Validation:TimeoutMs"] = "5000",
            ["Validation:CacheTtlMinutes"] = "0"
        });
        using var client = factory.CreateClient();

        factory.CnpjHandler.When("cnpj/v1/", HttpStatusCode.OK, CnpjMatchJson);
        factory.BrasilApiCepHandler.When("cep/v2/", HttpStatusCode.InternalServerError);
        factory.ViaCepHandler.When("/ws/", HttpStatusCode.OK, """
            {
                "uf": "SP",
                "localidade": "VINHEDO",
                "logradouro": "ESTRADA MUNICIPAL VINHEDO/ITUPEVA"
            }
            """);

        var response = await client.PostAsJsonAsync(Endpoint, new { cnpj = ValidCnpj, cep = ValidCep });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        factory.BrasilApiCepHandler.CallCount.Should().Be(3);
        factory.ViaCepHandler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Post_PrimaryCepTimeout_FallsBackToViaCep_Returns200()
    {
        using var factory = new CustomWebApplicationFactory(new Dictionary<string, string?>
        {
            ["Validation:CnpjRetryCount"] = "0",
            ["Validation:CepPrimaryRetryCount"] = "0",
            ["Validation:CepSecondaryRetryCount"] = "0",
            ["Validation:TimeoutMs"] = "100",
            ["Validation:CacheTtlMinutes"] = "0"
        });
        using var client = factory.CreateClient();

        factory.CnpjHandler.When("cnpj/v1/", HttpStatusCode.OK, CnpjMatchJson);
        factory.BrasilApiCepHandler.WhenAsync(
            "cep/v2/",
            _ => Task.FromException<HttpResponseMessage>(new TaskCanceledException("Simulated timeout")));
        factory.ViaCepHandler.When("/ws/", HttpStatusCode.OK, """
            {
                "uf": "SP",
                "localidade": "VINHEDO",
                "logradouro": "ESTRADA MUNICIPAL VINHEDO/ITUPEVA"
            }
            """);

        var response = await client.PostAsJsonAsync(Endpoint, new { cnpj = ValidCnpj, cep = ValidCep });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ValidateCustomerRegistrationResponse>();
        body!.CepProvider.Should().Be("ViaCep");
        factory.BrasilApiCepHandler.CallCount.Should().Be(1);
        factory.ViaCepHandler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Post_CircuitBreakerOpenedOnPrimary_SkipsPrimaryAndUsesFallback()
    {
        using var factory = new CustomWebApplicationFactory(new Dictionary<string, string?>
        {
            ["Validation:CnpjRetryCount"] = "0",
            ["Validation:CepPrimaryRetryCount"] = "0",
            ["Validation:CepSecondaryRetryCount"] = "0",
            ["Validation:CacheTtlMinutes"] = "0"
        });
        using var client = factory.CreateClient();

        factory.CnpjHandler.When("cnpj/v1/", HttpStatusCode.OK, CnpjMatchJson);
        factory.BrasilApiCepHandler.When("cep/v2/", HttpStatusCode.InternalServerError);
        factory.ViaCepHandler.When("/ws/", HttpStatusCode.OK, """
            {
                "uf": "SP",
                "localidade": "VINHEDO",
                "logradouro": "ESTRADA MUNICIPAL VINHEDO/ITUPEVA"
            }
            """);

        for (var i = 0; i < 6; i++)
        {
            var response = await client.PostAsJsonAsync(Endpoint, new { cnpj = ValidCnpj, cep = ValidCep });
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        factory.BrasilApiCepHandler.CallCount.Should().Be(5);
        factory.ViaCepHandler.CallCount.Should().Be(6);
    }

    [Fact]
    public async Task Post_PrimaryCepProviderCanBeConfiguredToViaCep()
    {
        using var factory = new CustomWebApplicationFactory(new Dictionary<string, string?>
        {
            ["Validation:PrimaryCepProvider"] = "ViaCep",
            ["Validation:CnpjRetryCount"] = "0",
            ["Validation:CepPrimaryRetryCount"] = "0",
            ["Validation:CepSecondaryRetryCount"] = "0",
            ["Validation:CacheTtlMinutes"] = "0"
        });
        using var client = factory.CreateClient();

        factory.CnpjHandler.When("cnpj/v1/", HttpStatusCode.OK, CnpjMatchJson);
        factory.ViaCepHandler.When("/ws/", HttpStatusCode.OK, """
            {
                "uf": "SP",
                "localidade": "VINHEDO",
                "logradouro": "ESTRADA MUNICIPAL VINHEDO/ITUPEVA"
            }
            """);

        var response = await client.PostAsJsonAsync(Endpoint, new { cnpj = ValidCnpj, cep = ValidCep });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ValidateCustomerRegistrationResponse>();
        body!.CepProvider.Should().Be("ViaCep");
        factory.ViaCepHandler.CallCount.Should().Be(1);
        factory.BrasilApiCepHandler.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Post_MissingFields_Returns400()
    {
        var response = await _client.PostAsJsonAsync(Endpoint, new { });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_CorrelationIdPropagated_ResponseHasHeader()
    {
        _factory.CnpjHandler.When("cnpj/v1/", HttpStatusCode.OK, CnpjMatchJson);
        _factory.BrasilApiCepHandler.When("cep/v2/", HttpStatusCode.OK, CepMatchJson);

        var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        request.Content = JsonContent.Create(new { cnpj = ValidCnpj, cep = ValidCep });
        request.Headers.Add("x-correlation-id", "test-correlation-123");

        var response = await _client.SendAsync(request);

        response.Headers.Should().ContainKey("x-correlation-id");
        response.Headers.GetValues("x-correlation-id").Should().Contain("test-correlation-123");
    }
}
