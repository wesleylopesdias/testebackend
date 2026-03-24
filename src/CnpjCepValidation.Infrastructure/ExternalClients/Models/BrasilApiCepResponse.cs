using System.Text.Json.Serialization;

namespace CnpjCepValidation.Infrastructure.ExternalClients.Models;

internal sealed class BrasilApiCepResponse
{
    [JsonPropertyName("state")]
    public string? State { get; init; }

    [JsonPropertyName("city")]
    public string? City { get; init; }

    [JsonPropertyName("street")]
    public string? Street { get; init; }
}
