namespace CnpjCepValidation.Application.DTOs;

public sealed record ValidateCustomerRegistrationRequest(string Cnpj, string Cep);
