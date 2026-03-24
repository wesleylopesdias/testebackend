using CnpjCepValidation.Application.Abstractions;
using CnpjCepValidation.Application.DTOs;
using CnpjCepValidation.Application.Exceptions;
using CnpjCepValidation.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace CnpjCepValidation.Api.Controllers;

[ApiController]
[Route("api/v1/customer-registration-validations")]
public sealed class CustomerRegistrationValidationsController : ControllerBase
{
    private readonly IRegistrationValidationUseCase _useCase;
    private readonly ILogger<CustomerRegistrationValidationsController> _logger;

    public CustomerRegistrationValidationsController(
        IRegistrationValidationUseCase useCase,
        ILogger<CustomerRegistrationValidationsController> logger)
    {
        _useCase = useCase;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ValidateCustomerRegistrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidateCustomerRegistrationResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidateCustomerRegistrationResponse), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Validate(
        [FromBody] ValidateCustomerRegistrationApiRequest request,
        CancellationToken cancellationToken)
    {
        if (!Cnpj.TryCreate(request.Cnpj ?? string.Empty, out _))
        {
            ModelState.AddModelError(nameof(request.Cnpj), "CNPJ invalido. Verifique o formato e os digitos verificadores.");
            return ValidationProblem(ModelState);
        }

        if (!Cep.TryCreate(request.Cep ?? string.Empty, out _))
        {
            ModelState.AddModelError(nameof(request.Cep), "CEP invalido. Deve conter 8 digitos.");
            return ValidationProblem(ModelState);
        }

        try
        {
            var result = await _useCase.ExecuteAsync(
                new ValidateCustomerRegistrationRequest(request.Cnpj!, request.Cep!),
                cancellationToken);

            return result.Reason switch
            {
                ValidationReason.Match => Ok(result),
                ValidationReason.AddressMismatch
                    or ValidationReason.CompanyNotFound
                    or ValidationReason.PostalCodeNotFound => NotFound(result),
                _ => Ok(result)
            };
        }
        catch (DependencyUnavailableException ex)
        {
            _logger.LogError(
                ex,
                "Dependencia indisponivel ao validar CNPJ {Cnpj} / CEP {Cep}",
                request.Cnpj,
                request.Cep);

            var errorResponse = new ValidateCustomerRegistrationResponse(
                IsMatch: false,
                Reason: ValidationReason.DependencyUnavailable,
                CepProvider: string.Empty,
                CompanyAddress: new ComparableAddressDto(string.Empty, string.Empty, string.Empty),
                PostalAddress: new ComparableAddressDto(string.Empty, string.Empty, string.Empty));

            return StatusCode(StatusCodes.Status503ServiceUnavailable, errorResponse);
        }
    }
}

public sealed record ValidateCustomerRegistrationApiRequest(string? Cnpj, string? Cep);
