using CnpjCepValidation.Application.Abstractions;
using CnpjCepValidation.Application.Exceptions;
using CnpjCepValidation.Application.Models;
using CnpjCepValidation.Domain.ValueObjects;
using CnpjCepValidation.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CnpjCepValidation.Unit.Infrastructure;

public sealed class CepAddressResolverTests
{
    private readonly Mock<ICepAddressProvider> _primary = new();
    private readonly Mock<ICepAddressProvider> _secondary = new();
    private readonly CepAddressResolver _sut;
    private readonly Cep _validCep = Cep.Create("13288390");

    public CepAddressResolverTests()
    {
        _primary.Setup(p => p.ProviderName).Returns("BrasilApi");
        _secondary.Setup(s => s.ProviderName).Returns("ViaCep");

        _sut = new CepAddressResolver(
            _primary.Object,
            _secondary.Object,
            NullLogger<CepAddressResolver>.Instance);
    }

    [Fact]
    public async Task ResolveAsync_PrimarySucceeds_ReturnsPrimaryResult()
    {
        var address = new CepAddressInfo("SP", "Vinhedo", "Rodovia dos Bandeirantes");

        _primary
            .Setup(p => p.GetAddressAsync(_validCep, It.IsAny<CancellationToken>()))
            .ReturnsAsync(address);

        var result = await _sut.ResolveAsync(_validCep, CancellationToken.None);

        result.Address.Should().Be(address);
        result.ProviderName.Should().Be("BrasilApi");
        _secondary.Verify(
            s => s.GetAddressAsync(It.IsAny<Cep>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_PrimaryReturnsNull_DoesNotFallback()
    {
        _primary
            .Setup(p => p.GetAddressAsync(_validCep, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CepAddressInfo?)null);

        var result = await _sut.ResolveAsync(_validCep, CancellationToken.None);

        result.Address.Should().BeNull();
        result.ProviderName.Should().Be("BrasilApi");
        _secondary.Verify(
            s => s.GetAddressAsync(It.IsAny<Cep>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_PrimaryFails_FallsBackToSecondary()
    {
        var address = new CepAddressInfo("SP", "Vinhedo", "Rodovia dos Bandeirantes");

        _primary
            .Setup(p => p.GetAddressAsync(_validCep, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DependencyUnavailableException("Primary down"));

        _secondary
            .Setup(s => s.GetAddressAsync(_validCep, It.IsAny<CancellationToken>()))
            .ReturnsAsync(address);

        var result = await _sut.ResolveAsync(_validCep, CancellationToken.None);

        result.Address.Should().Be(address);
        result.ProviderName.Should().Be("ViaCep");
    }

    [Fact]
    public async Task ResolveAsync_BothFail_ThrowsDependencyUnavailableException()
    {
        _primary
            .Setup(p => p.GetAddressAsync(_validCep, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DependencyUnavailableException("Primary down"));

        _secondary
            .Setup(s => s.GetAddressAsync(_validCep, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DependencyUnavailableException("Secondary down"));

        var act = async () => await _sut.ResolveAsync(_validCep, CancellationToken.None);

        await act.Should().ThrowAsync<DependencyUnavailableException>();
    }
}
