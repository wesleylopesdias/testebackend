using System.ComponentModel.DataAnnotations;

namespace CnpjCepValidation.Infrastructure.Options;

public sealed class ExternalApiOptions
{
    public const string SectionName = "ExternalApis";

    [Required]
    [Url]
    public string BrasilApiBaseUrl { get; set; } = "https://brasilapi.com.br";

    [Required]
    [Url]
    public string ViaCepBaseUrl { get; set; } = "https://viacep.com.br";
}
