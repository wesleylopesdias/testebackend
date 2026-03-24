using CnpjCepValidation.Application.Abstractions;
using CnpjCepValidation.Application.DTOs;
using CnpjCepValidation.Application.Exceptions;
using CnpjCepValidation.Application.Models;
using CnpjCepValidation.Application.Options;
using CnpjCepValidation.Application.UseCases;
using CnpjCepValidation.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace CnpjCepValidation.Unit.Application;

public sealed class ValidateCustomerRegistrationUseCaseTests
{
    private const string ValidCnpj = "00924432000199";
    private const string ValidCep = "13288390";

    private readonly Mock<ICompanyRegistryClient> _companyClient = new();
    private readonly Mock<ICepAddressResolver> _cepResolver = new();
    private readonly Mock<IAddressComparer> _addressComparer = new();
    private readonly IMemoryCache _cache;
    private readonly ValidateCustomerRegistrationUseCase _sut;

    public ValidateCustomerRegistrationUseCaseTests()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new ValidationOptions { CacheTtlMinutes = 5 });

        _sut = new ValidateCustomerRegistrationUseCase(
            _companyClient.Object,
            _cepResolver.Object,
            _addressComparer.Object,
            _cache,
            NullLogger<ValidateCustomerRegistrationUseCase>.Instance,
            options);
    }

    [Fact]
    public async Task ExecuteAsync_MatchingAddresses_ReturnsMatchResponse()
    {
        var company = new CompanyInfo("SP", "Vinhedo", "Estrada Municipal Vinhedo Itupeva");
        var cepResult = new CepResolveResult(
            new CepAddressInfo("SP", "Vinhedo", "Estrada Municipal Vinhedo Itupeva"),
            "BrasilApi");

        _companyClient
            .Setup(c => c.GetCompanyAsync(It.IsAny<Cnpj>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(company);

        _cepResolver
            .Setup(r => r.ResolveAsync(It.IsAny<Cep>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cepResult);

        _addressComparer
            .Setup(c => c.AreEqual(It.IsAny<ComparableAddress>(), It.IsAny<ComparableAddress>()))
            .Returns(true);

        var result = await _sut.ExecuteAsync(
            new ValidateCustomerRegistrationRequest(ValidCnpj, ValidCep),
            CancellationToken.None);

        result.IsMatch.Should().BeTrue();
        result.Reason.Should().Be(ValidationReason.Match);
        result.CepProvider.Should().Be("BrasilApi");
    }

    [Fact]
    public async Task ExecuteAsync_MismatchingAddresses_ReturnsMismatchResponse()
    {
        _companyClient
            .Setup(c => c.GetCompanyAsync(It.IsAny<Cnpj>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompanyInfo("SP", "Vinhedo", "Estrada Velha"));

        _cepResolver
            .Setup(r => r.ResolveAsync(It.IsAny<Cep>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CepResolveResult(
                new CepAddressInfo("SP", "Campinas", "Rodovia dos Bandeirantes"), "BrasilApi"));

        _addressComparer
            .Setup(c => c.AreEqual(It.IsAny<ComparableAddress>(), It.IsAny<ComparableAddress>()))
            .Returns(false);

        var result = await _sut.ExecuteAsync(
            new ValidateCustomerRegistrationRequest(ValidCnpj, ValidCep),
            CancellationToken.None);

        result.IsMatch.Should().BeFalse();
        result.Reason.Should().Be(ValidationReason.AddressMismatch);
    }

    [Fact]
    public async Task ExecuteAsync_CompanyNotFound_ReturnsCompanyNotFoundResponse()
    {
        _companyClient
            .Setup(c => c.GetCompanyAsync(It.IsAny<Cnpj>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompanyInfo?)null);

        var result = await _sut.ExecuteAsync(
            new ValidateCustomerRegistrationRequest(ValidCnpj, ValidCep),
            CancellationToken.None);

        result.IsMatch.Should().BeFalse();
        result.Reason.Should().Be(ValidationReason.CompanyNotFound);
        _cepResolver.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_CepNotFound_ReturnsPostalCodeNotFoundResponse()
    {
        _companyClient
            .Setup(c => c.GetCompanyAsync(It.IsAny<Cnpj>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompanyInfo("SP", "Vinhedo", "Estrada"));

        _cepResolver
            .Setup(r => r.ResolveAsync(It.IsAny<Cep>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CepResolveResult(null, "BrasilApi"));

        var result = await _sut.ExecuteAsync(
            new ValidateCustomerRegistrationRequest(ValidCnpj, ValidCep),
            CancellationToken.None);

        result.IsMatch.Should().BeFalse();
        result.Reason.Should().Be(ValidationReason.PostalCodeNotFound);
    }

    [Fact]
    public async Task ExecuteAsync_CompanyRegistryUnavailable_ThrowsDependencyUnavailableException()
    {
        _companyClient
            .Setup(c => c.GetCompanyAsync(It.IsAny<Cnpj>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DependencyUnavailableException("CNPJ fora do ar"));

        var act = async () => await _sut.ExecuteAsync(
            new ValidateCustomerRegistrationRequest(ValidCnpj, ValidCep),
            CancellationToken.None);

        await act.Should().ThrowAsync<DependencyUnavailableException>();
    }

    [Fact]
    public async Task ExecuteAsync_CepResolverUnavailable_ThrowsDependencyUnavailableException()
    {
        _companyClient
            .Setup(c => c.GetCompanyAsync(It.IsAny<Cnpj>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompanyInfo("SP", "Vinhedo", "Estrada"));

        _cepResolver
            .Setup(r => r.ResolveAsync(It.IsAny<Cep>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DependencyUnavailableException("CEP fora do ar"));

        var act = async () => await _sut.ExecuteAsync(
            new ValidateCustomerRegistrationRequest(ValidCnpj, ValidCep),
            CancellationToken.None);

        await act.Should().ThrowAsync<DependencyUnavailableException>();
    }

    [Fact]
    public async Task ExecuteAsync_SecondCallWithSameCnpj_UsesCache()
    {
        var company = new CompanyInfo("SP", "Vinhedo", "Estrada");

        _companyClient
            .Setup(c => c.GetCompanyAsync(It.IsAny<Cnpj>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(company);

        _cepResolver
            .Setup(r => r.ResolveAsync(It.IsAny<Cep>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CepResolveResult(
                new CepAddressInfo("SP", "Vinhedo", "Estrada"),
                "BrasilApi"));

        _addressComparer
            .Setup(c => c.AreEqual(It.IsAny<ComparableAddress>(), It.IsAny<ComparableAddress>()))
            .Returns(true);

        await _sut.ExecuteAsync(
            new ValidateCustomerRegistrationRequest(ValidCnpj, ValidCep),
            CancellationToken.None);

        await _sut.ExecuteAsync(
            new ValidateCustomerRegistrationRequest(ValidCnpj, ValidCep),
            CancellationToken.None);

        _companyClient.Verify(
            c => c.GetCompanyAsync(It.IsAny<Cnpj>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_AddressMismatch_DoesNotAttemptFallback()
    {
        _companyClient
            .Setup(c => c.GetCompanyAsync(It.IsAny<Cnpj>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompanyInfo("SP", "Vinhedo", "Estrada A"));

        _cepResolver
            .Setup(r => r.ResolveAsync(It.IsAny<Cep>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CepResolveResult(
                new CepAddressInfo("SP", "Campinas", "Rua B"),
                "BrasilApi"));

        _addressComparer
            .Setup(c => c.AreEqual(It.IsAny<ComparableAddress>(), It.IsAny<ComparableAddress>()))
            .Returns(false);

        var result = await _sut.ExecuteAsync(
            new ValidateCustomerRegistrationRequest(ValidCnpj, ValidCep),
            CancellationToken.None);

        result.Reason.Should().Be(ValidationReason.AddressMismatch);
        _cepResolver.Verify(
            r => r.ResolveAsync(It.IsAny<Cep>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
