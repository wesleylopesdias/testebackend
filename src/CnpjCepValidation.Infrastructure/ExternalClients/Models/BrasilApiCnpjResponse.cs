using System.Text.Json.Serialization;

namespace CnpjCepValidation.Infrastructure.ExternalClients.Models;

internal sealed class BrasilApiCnpjResponse
{
    [JsonPropertyName("uf")]
    public string? Uf { get; init; }

    [JsonPropertyName("municipio")]
    public string? Municipio { get; init; }

    [JsonPropertyName("logradouro")]
    public string? Logradouro { get; init; }

    [JsonPropertyName("descricao_tipo_de_logradouro")]
    public string? DescricaoTipoDeLogradouro { get; init; }
}
