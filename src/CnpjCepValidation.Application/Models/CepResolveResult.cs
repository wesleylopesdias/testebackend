namespace CnpjCepValidation.Application.Models;

public sealed record CepResolveResult(CepAddressInfo? Address, string ProviderName);
