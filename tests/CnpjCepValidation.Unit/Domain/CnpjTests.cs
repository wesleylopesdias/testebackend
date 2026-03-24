using CnpjCepValidation.Domain.ValueObjects;
using FluentAssertions;

namespace CnpjCepValidation.Unit.Domain;

public sealed class CnpjTests
{
    [Theory]
    [InlineData("00924432000199")]
    [InlineData("11222333000181")]
    public void Create_ValidCnpj_ReturnsInstance(string input)
    {
        var cnpj = Cnpj.Create(input);
        cnpj.Should().NotBeNull();
        cnpj.Value.Should().Be(new string(input.Where(char.IsDigit).ToArray()));
    }

    [Theory]
    [InlineData("00.924.432/0001-99")]
    [InlineData("00924432000199")]
    public void Create_AcceptsMaskedAndUnmasked(string input)
    {
        var cnpj = Cnpj.Create(input);
        cnpj.Value.Should().Be("00924432000199");
    }

    [Theory]
    [InlineData("00000000000000")]
    [InlineData("11111111111111")]
    public void Create_AllSameDigits_ThrowsArgumentException(string input)
    {
        var act = () => Cnpj.Create(input);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("1234")]
    [InlineData("123456789012345")]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_InvalidFormat_ThrowsArgumentException(string input)
    {
        var act = () => Cnpj.Create(input);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_InvalidCheckDigits_ThrowsArgumentException()
    {
        var act = () => Cnpj.Create("00924432000198");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TryCreate_ValidCnpj_ReturnsTrueAndInstance()
    {
        var result = Cnpj.TryCreate("00924432000199", out var cnpj);
        result.Should().BeTrue();
        cnpj.Should().NotBeNull();
    }

    [Fact]
    public void TryCreate_InvalidCnpj_ReturnsFalseAndNull()
    {
        var result = Cnpj.TryCreate("invalid", out var cnpj);
        result.Should().BeFalse();
        cnpj.Should().BeNull();
    }
}
