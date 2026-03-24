using System.Diagnostics;
using CnpjCepValidation.Application.Abstractions;
using CnpjCepValidation.Application.Diagnostics;
using CnpjCepValidation.Application.DTOs;
using CnpjCepValidation.Application.Exceptions;
using CnpjCepValidation.Application.Models;
using CnpjCepValidation.Application.Options;
using CnpjCepValidation.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CnpjCepValidation.Application.UseCases;

public sealed class ValidateCustomerRegistrationUseCase : IRegistrationValidationUseCase
{
    private readonly ICompanyRegistryClient _companyRegistry;
    private readonly ICepAddressResolver _cepResolver;
    private readonly IAddressComparer _addressComparer;
    private readonly IValidationCache _cache;
    private readonly ILogger<ValidateCustomerRegistrationUseCase> _logger;
    private readonly ValidationOptions _options;

    public ValidateCustomerRegistrationUseCase(
        ICompanyRegistryClient companyRegistry,
        ICepAddressResolver cepResolver,
        IAddressComparer addressComparer,
        IValidationCache cache,
        ILogger<ValidateCustomerRegistrationUseCase> logger,
        IOptions<ValidationOptions> options)
    {
        _companyRegistry = companyRegistry;
        _cepResolver = cepResolver;
        _addressComparer = addressComparer;
        _cache = cache;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<ValidateCustomerRegistrationResponse> ExecuteAsync(
        ValidateCustomerRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        var cnpj = Cnpj.Create(request.Cnpj);
        var cep = Cep.Create(request.Cep);
        using var activity = ValidationDiagnostics.StartValidationActivity(cnpj.Value, cep.Value);
        var stopwatch = Stopwatch.StartNew();
        string outcome = ValidationReason.DependencyUnavailable;

        try
        {
            var company = await GetCompanyWithCacheAsync(cnpj, cancellationToken);
            if (company is null)
            {
                outcome = ValidationReason.CompanyNotFound;
                activity?.SetTag("validation.reason", outcome);
                _logger.LogInformation("Empresa nao encontrada para CNPJ {CnpjMasked}", MaskCnpj(cnpj.Value));
                return BuildNotFoundResponse(outcome);
            }

            var cepResult = await GetCepWithCacheAsync(cep, cancellationToken);
            if (cepResult.Address is null)
            {
                outcome = ValidationReason.PostalCodeNotFound;
                activity?.SetTag("validation.reason", outcome);
                activity?.SetTag("validation.cep_provider", cepResult.ProviderName);
                _logger.LogInformation("CEP nao encontrado: {Cep}", cep.Value);
                return BuildNotFoundResponse(outcome, cepResult.ProviderName);
            }

            var companyAddress = new ComparableAddress(company.State, company.City, company.Street);
            var postalAddress = new ComparableAddress(
                cepResult.Address.State,
                cepResult.Address.City,
                cepResult.Address.Street);

            bool isMatch = _addressComparer.AreEqual(companyAddress, postalAddress);
            outcome = isMatch ? ValidationReason.Match : ValidationReason.AddressMismatch;

            activity?.SetTag("validation.reason", outcome);
            activity?.SetTag("validation.is_match", isMatch);
            activity?.SetTag("validation.cep_provider", cepResult.ProviderName);
            activity?.SetStatus(ActivityStatusCode.Ok);

            _logger.LogInformation(
                "Resultado da validacao para CNPJ {CnpjMasked} / CEP {Cep}: {Reason}",
                MaskCnpj(cnpj.Value),
                cep.Value,
                outcome);

            return new ValidateCustomerRegistrationResponse(
                IsMatch: isMatch,
                Reason: outcome,
                CepProvider: cepResult.ProviderName,
                CompanyAddress: ToDto(companyAddress),
                PostalAddress: ToDto(postalAddress));
        }
        catch (DependencyUnavailableException ex)
        {
            outcome = ValidationReason.DependencyUnavailable;
            activity?.SetTag("validation.reason", outcome);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
        finally
        {
            ValidationDiagnostics.RecordOutcome(outcome);
            ValidationDiagnostics.RecordDuration(stopwatch.Elapsed, outcome);
        }
    }

    private async Task<CompanyInfo?> GetCompanyWithCacheAsync(Cnpj cnpj, CancellationToken ct)
    {
        var cacheKey = $"cnpj:{cnpj.Value}";
        if (_cache.TryGet(cacheKey, out CompanyInfo? cached))
        {
            _logger.LogDebug("Cache hit para CNPJ {CnpjMasked}", MaskCnpj(cnpj.Value));
            return cached;
        }

        var negativeCacheKey = $"cnpj:notfound:{cnpj.Value}";
        if (_cache.TryGet<bool>(negativeCacheKey, out _))
        {
            _logger.LogDebug("Cache hit (negativo) para CNPJ {CnpjMasked}", MaskCnpj(cnpj.Value));
            return null;
        }

        var company = await _companyRegistry.GetCompanyAsync(cnpj, ct);

        if (company is not null && _options.CacheTtlMinutes > 0)
        {
            _cache.Set(cacheKey, company, TimeSpan.FromMinutes(_options.CacheTtlMinutes));
        }
        else if (company is null && _options.NegativeCacheTtlMinutes > 0)
        {
            _cache.Set(negativeCacheKey, true, TimeSpan.FromMinutes(_options.NegativeCacheTtlMinutes));
        }

        return company;
    }

    private async Task<CepResolveResult> GetCepWithCacheAsync(Cep cep, CancellationToken ct)
    {
        var cacheKey = $"cep:{cep.Value}";
        if (_cache.TryGet(cacheKey, out CepResolveResult? cached) && cached is not null)
        {
            _logger.LogDebug("Cache hit para CEP {Cep}", cep.Value);
            return cached;
        }

        var negativeCacheKey = $"cep:notfound:{cep.Value}";
        if (_cache.TryGet(negativeCacheKey, out CepResolveResult? negativeCached) && negativeCached is not null)
        {
            _logger.LogDebug("Cache hit (negativo) para CEP {Cep}", cep.Value);
            return negativeCached;
        }

        var result = await _cepResolver.ResolveAsync(cep, ct);

        if (result.Address is not null && _options.CacheTtlMinutes > 0)
        {
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(_options.CacheTtlMinutes));
        }
        else if (result.Address is null && _options.NegativeCacheTtlMinutes > 0)
        {
            _cache.Set(negativeCacheKey, result, TimeSpan.FromMinutes(_options.NegativeCacheTtlMinutes));
        }

        return result;
    }

    private static ValidateCustomerRegistrationResponse BuildNotFoundResponse(
        string reason,
        string cepProvider = "")
    {
        var empty = new ComparableAddressDto(string.Empty, string.Empty, string.Empty);
        return new ValidateCustomerRegistrationResponse(
            IsMatch: false,
            Reason: reason,
            CepProvider: cepProvider,
            CompanyAddress: empty,
            PostalAddress: empty);
    }

    private static ComparableAddressDto ToDto(ComparableAddress address) =>
        new(address.State, address.City, address.Street);

    private static string MaskCnpj(string cnpj)
    {
        if (cnpj.Length != 14) return "***";
        return $"{cnpj[..2]}.{cnpj[2..5]}.***/{cnpj[8..12]}-**";
    }
}
