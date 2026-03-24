using System.ComponentModel.DataAnnotations;

namespace CnpjCepValidation.Application.Options;

public sealed class ValidationOptions
{
    public const string SectionName = "Validation";

    [Required]
    [RegularExpression("^(BrasilApi|ViaCep)$", ErrorMessage = "PrimaryCepProvider deve ser 'BrasilApi' ou 'ViaCep'.")]
    public string PrimaryCepProvider { get; set; } = "BrasilApi";

    [Range(0, 1440)]
    public int CacheTtlMinutes { get; set; } = 5;

    [Range(0, 1440)]
    public int NegativeCacheTtlMinutes { get; set; } = 1;

    [Range(100, 30000)]
    public int TimeoutMs { get; set; } = 800;

    [Range(0, 10)]
    public int CnpjRetryCount { get; set; } = 2;

    [Range(0, 10)]
    public int CepPrimaryRetryCount { get; set; } = 2;

    [Range(0, 10)]
    public int CepSecondaryRetryCount { get; set; } = 1;
}
