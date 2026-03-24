namespace CnpjCepValidation.Application.Options;

public sealed class ValidationOptions
{
    public const string SectionName = "Validation";

    public string PrimaryCepProvider { get; set; } = "BrasilApi";
    public int CacheTtlMinutes { get; set; } = 5;
    public int TimeoutMs { get; set; } = 800;
    public int CnpjRetryCount { get; set; } = 2;
    public int CepPrimaryRetryCount { get; set; } = 2;
    public int CepSecondaryRetryCount { get; set; } = 1;
}
