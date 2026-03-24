namespace CnpjCepValidation.Infrastructure.Options;

public sealed class ExternalApiOptions
{
    public const string SectionName = "ExternalApis";

    public string BrasilApiBaseUrl { get; set; } = "https://brasilapi.com.br";
    public string ViaCepBaseUrl { get; set; } = "https://viacep.com.br";
}
