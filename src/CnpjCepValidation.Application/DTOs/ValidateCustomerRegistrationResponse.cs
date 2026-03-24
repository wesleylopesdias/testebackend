namespace CnpjCepValidation.Application.DTOs;

public sealed record ValidateCustomerRegistrationResponse(
    bool IsMatch,
    string Reason,
    string CepProvider,
    ComparableAddressDto CompanyAddress,
    ComparableAddressDto PostalAddress);

public static class ValidationReason
{
    public const string Match = "Match";
    public const string AddressMismatch = "AddressMismatch";
    public const string CompanyNotFound = "CompanyNotFound";
    public const string PostalCodeNotFound = "PostalCodeNotFound";
    public const string DependencyUnavailable = "DependencyUnavailable";
}
