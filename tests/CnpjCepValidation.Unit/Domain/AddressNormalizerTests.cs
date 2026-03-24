using CnpjCepValidation.Domain.Services;
using FluentAssertions;

namespace CnpjCepValidation.Unit.Domain;

public sealed class AddressNormalizerTests
{
    [Theory]
    [InlineData("São Paulo", "SAO PAULO")]
    [InlineData("Vinhedo", "VINHEDO")]
    [InlineData("Rodovia dos Bandeirantes", "RODOVIA DOS BANDEIRANTES")]
    [InlineData("ESTRADA MUNICIPAL VINHEDO/ITUPEVA", "ESTRADA MUNICIPAL VINHEDO ITUPEVA")]
    [InlineData("  Av.  Brasil  ", "AV BRASIL")]
    [InlineData("Rua João Pessoa, 123", "RUA JOAO PESSOA 123")]
    public void Normalize_AppliesAllTransformations(string input, string expected)
    {
        AddressNormalizer.Normalize(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Normalize_EmptyOrNull_ReturnsEmpty(string? input)
    {
        AddressNormalizer.Normalize(input!).Should().BeEmpty();
    }

    [Fact]
    public void Normalize_SameTextDifferentAccents_ReturnsSameResult()
    {
        var a = AddressNormalizer.Normalize("Córrego das Flores");
        var b = AddressNormalizer.Normalize("Corrego das Flores");
        a.Should().Be(b);
    }
}
