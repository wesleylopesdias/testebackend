using System.Text.Json;
using CnpjCepValidation.Infrastructure.ExternalClients.Models;
using FluentAssertions;

namespace CnpjCepValidation.Unit.Infrastructure;

public sealed class ExternalApiContractTests
{
    [Fact]
    public void BrasilApiCnpjResponse_DeserializesExpectedSchema()
    {
        const string json = """
            {
                "uf": "SP",
                "municipio": "VINHEDO",
                "logradouro": "MUNICIPAL VINHEDO/ITUPEVA",
                "descricao_tipo_de_logradouro": "ESTRADA",
                "cnpj": "00924432000199",
                "razao_social": "EMPRESA TESTE LTDA"
            }
            """;

        var result = JsonSerializer.Deserialize<BrasilApiCnpjResponse>(json);

        result.Should().NotBeNull();
        result!.Uf.Should().Be("SP");
        result.Municipio.Should().Be("VINHEDO");
        result.Logradouro.Should().Be("MUNICIPAL VINHEDO/ITUPEVA");
        result.DescricaoTipoDeLogradouro.Should().Be("ESTRADA");
    }

    [Fact]
    public void BrasilApiCnpjResponse_MissingFields_DeserializesAsNull()
    {
        const string json = """
            {
                "uf": "SP"
            }
            """;

        var result = JsonSerializer.Deserialize<BrasilApiCnpjResponse>(json);

        result.Should().NotBeNull();
        result!.Uf.Should().Be("SP");
        result.Municipio.Should().BeNull();
        result.Logradouro.Should().BeNull();
        result.DescricaoTipoDeLogradouro.Should().BeNull();
    }

    [Fact]
    public void BrasilApiCepResponse_DeserializesExpectedSchema()
    {
        const string json = """
            {
                "cep": "13288390",
                "state": "SP",
                "city": "Vinhedo",
                "street": "Estrada Municipal Vinhedo/Itupeva",
                "neighborhood": "Distrito Industrial"
            }
            """;

        var result = JsonSerializer.Deserialize<BrasilApiCepResponse>(json);

        result.Should().NotBeNull();
        result!.State.Should().Be("SP");
        result.City.Should().Be("Vinhedo");
        result.Street.Should().Be("Estrada Municipal Vinhedo/Itupeva");
    }

    [Fact]
    public void BrasilApiCepResponse_MissingFields_DeserializesAsNull()
    {
        const string json = """
            {
                "state": "SP"
            }
            """;

        var result = JsonSerializer.Deserialize<BrasilApiCepResponse>(json);

        result.Should().NotBeNull();
        result!.State.Should().Be("SP");
        result.City.Should().BeNull();
        result.Street.Should().BeNull();
    }

    [Fact]
    public void ViaCepResponse_DeserializesExpectedSchema()
    {
        const string json = """
            {
                "cep": "13288-390",
                "logradouro": "Estrada Municipal Vinhedo/Itupeva",
                "complemento": "",
                "unidade": "",
                "bairro": "Distrito Industrial",
                "localidade": "Vinhedo",
                "uf": "SP",
                "estado": "São Paulo",
                "regiao": "Sudeste",
                "ibge": "3556701",
                "gia": "7180",
                "ddd": "19",
                "siafi": "7235"
            }
            """;

        var result = JsonSerializer.Deserialize<ViaCepResponse>(json);

        result.Should().NotBeNull();
        result!.Uf.Should().Be("SP");
        result.Localidade.Should().Be("Vinhedo");
        result.Logradouro.Should().Be("Estrada Municipal Vinhedo/Itupeva");
        result.Erro.Should().BeNull();
    }

    [Fact]
    public void ViaCepResponse_NotFound_DeserializesErroFlag()
    {
        const string json = """
            {
                "erro": true
            }
            """;

        var result = JsonSerializer.Deserialize<ViaCepResponse>(json);

        result.Should().NotBeNull();
        result!.Erro.Should().BeTrue();
        result.Uf.Should().BeNull();
        result.Localidade.Should().BeNull();
        result.Logradouro.Should().BeNull();
    }

    [Fact]
    public void ViaCepResponse_MissingFields_DeserializesAsNull()
    {
        const string json = """
            {
                "uf": "SP"
            }
            """;

        var result = JsonSerializer.Deserialize<ViaCepResponse>(json);

        result.Should().NotBeNull();
        result!.Uf.Should().Be("SP");
        result.Localidade.Should().BeNull();
        result.Logradouro.Should().BeNull();
        result.Erro.Should().BeNull();
    }
}
