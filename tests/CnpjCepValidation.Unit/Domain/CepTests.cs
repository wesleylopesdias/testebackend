using CnpjCepValidation.Domain.ValueObjects;
using FluentAssertions;

namespace CnpjCepValidation.Unit.Domain;

public sealed class CepTests
{
    [Theory]
    [InlineData("13288390", "13288390")]
    [InlineData("13288-390", "13288390")]
    [InlineData("01310100", "01310100")]
    public void Create_ValidCep_ReturnsDigitsOnly(string input, string expected)
    {
        var cep = Cep.Create(input);
        cep.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData("1234")]
    [InlineData("123456789")]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_InvalidFormat_ThrowsArgumentException(string input)
    {
        var act = () => Cep.Create(input);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TryCreate_ValidCep_ReturnsTrueAndInstance()
    {
        var result = Cep.TryCreate("13288390", out var cep);
        result.Should().BeTrue();
        cep.Should().NotBeNull();
    }

    [Fact]
    public void TryCreate_InvalidCep_ReturnsFalseAndNull()
    {
        var result = Cep.TryCreate("invalid", out var cep);
        result.Should().BeFalse();
        cep.Should().BeNull();
    }
}
