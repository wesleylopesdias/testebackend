using CnpjCepValidation.Application.DTOs;

namespace CnpjCepValidation.Application.Abstractions;

public interface IRegistrationValidationUseCase
{
    Task<ValidateCustomerRegistrationResponse> ExecuteAsync(
        ValidateCustomerRegistrationRequest request,
        CancellationToken cancellationToken);
}
