using CnpjCepValidation.Application.Services;
using CnpjCepValidation.Domain.ValueObjects;
using FluentAssertions;

namespace CnpjCepValidation.Unit.Application;

public sealed class AddressComparerTests
{
    private readonly AddressComparer _comparer = new();

    [Fact]
    public void AreEqual_IdenticalAddresses_ReturnsTrue()
    {
        var a = new ComparableAddress("SP", "Vinhedo", "Rodovia dos Bandeirantes");
        var b = new ComparableAddress("SP", "Vinhedo", "Rodovia dos Bandeirantes");
        _comparer.AreEqual(a, b).Should().BeTrue();
    }

    [Fact]
    public void AreEqual_DifferentCasing_ReturnsTrue()
    {
        var a = new ComparableAddress("SP", "vinhedo", "rodovia dos bandeirantes");
        var b = new ComparableAddress("sp", "VINHEDO", "RODOVIA DOS BANDEIRANTES");
        _comparer.AreEqual(a, b).Should().BeTrue();
    }

    [Fact]
    public void AreEqual_AccentDifference_ReturnsTrue()
    {
        var a = new ComparableAddress("SP", "São Paulo", "Avenida Brigadeiro Faria Lima");
        var b = new ComparableAddress("SP", "Sao Paulo", "Avenida Brigadeiro Faria Lima");
        _comparer.AreEqual(a, b).Should().BeTrue();
    }

    [Fact]
    public void AreEqual_PunctuationDifference_ReturnsTrue()
    {
        var a = new ComparableAddress("SP", "Vinhedo", "Estrada Mun. Vinhedo/Itupeva");
        var b = new ComparableAddress("SP", "Vinhedo", "Estrada Mun Vinhedo Itupeva");
        _comparer.AreEqual(a, b).Should().BeTrue();
    }

    [Fact]
    public void AreEqual_DifferentState_ReturnsFalse()
    {
        var a = new ComparableAddress("SP", "Vinhedo", "Rodovia dos Bandeirantes");
        var b = new ComparableAddress("RJ", "Vinhedo", "Rodovia dos Bandeirantes");
        _comparer.AreEqual(a, b).Should().BeFalse();
    }

    [Fact]
    public void AreEqual_DifferentCity_ReturnsFalse()
    {
        var a = new ComparableAddress("SP", "Vinhedo", "Rodovia dos Bandeirantes");
        var b = new ComparableAddress("SP", "Campinas", "Rodovia dos Bandeirantes");
        _comparer.AreEqual(a, b).Should().BeFalse();
    }

    [Fact]
    public void AreEqual_DifferentStreet_ReturnsFalse()
    {
        var a = new ComparableAddress("SP", "Vinhedo", "Rodovia dos Bandeirantes");
        var b = new ComparableAddress("SP", "Vinhedo", "Rua das Flores");
        _comparer.AreEqual(a, b).Should().BeFalse();
    }
}
