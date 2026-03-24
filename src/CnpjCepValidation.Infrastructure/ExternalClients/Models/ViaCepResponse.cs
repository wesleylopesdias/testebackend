using System.Text.Json.Serialization;

namespace CnpjCepValidation.Infrastructure.ExternalClients.Models;

internal sealed class ViaCepResponse
{
    [JsonPropertyName("uf")]
    public string? Uf { get; init; }

    [JsonPropertyName("localidade")]
    public string? Localidade { get; init; }

    [JsonPropertyName("logradouro")]
    public string? Logradouro { get; init; }

    [JsonPropertyName("erro")]
    public bool? Erro { get; init; }
}
