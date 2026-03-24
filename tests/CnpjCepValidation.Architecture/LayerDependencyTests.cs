using FluentAssertions;
using NetArchTest.Rules;

namespace CnpjCepValidation.Architecture;

public sealed class LayerDependencyTests
{
    private const string DomainNs = "CnpjCepValidation.Domain";
    private const string ApplicationNs = "CnpjCepValidation.Application";
    private const string InfrastructureNs = "CnpjCepValidation.Infrastructure";
    private const string ApiNs = "CnpjCepValidation.Api";

    [Fact]
    public void Domain_ShouldNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(typeof(CnpjCepValidation.Domain.ValueObjects.Cnpj).Assembly)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Domain must not reference Infrastructure");
    }

    [Fact]
    public void Domain_ShouldNotDependOn_Api()
    {
        var result = Types.InAssembly(typeof(CnpjCepValidation.Domain.ValueObjects.Cnpj).Assembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Domain must not reference Api");
    }

    [Fact]
    public void Application_ShouldNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(typeof(CnpjCepValidation.Application.Abstractions.ICompanyRegistryClient).Assembly)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Application must not reference Infrastructure");
    }

    [Fact]
    public void Application_ShouldNotDependOn_Api()
    {
        var result = Types.InAssembly(typeof(CnpjCepValidation.Application.Abstractions.ICompanyRegistryClient).Assembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Application must not reference Api");
    }

    [Fact]
    public void Infrastructure_ShouldNotDependOn_Api()
    {
        var result = Types.InAssembly(typeof(CnpjCepValidation.Infrastructure.ExternalClients.BrasilApiCnpjClient).Assembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Infrastructure must not reference Api");
    }

    [Fact]
    public void DomainValueObjects_ShouldBeSealed()
    {
        var result = Types.InAssembly(typeof(CnpjCepValidation.Domain.ValueObjects.Cnpj).Assembly)
            .That()
            .ResideInNamespace($"{DomainNs}.ValueObjects")
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "value objects should be sealed to prevent inheritance");
    }

    [Fact]
    public void ApplicationInterfaces_ShouldResideInAbstractionsNamespace()
    {
        var result = Types.InAssembly(typeof(CnpjCepValidation.Application.Abstractions.ICompanyRegistryClient).Assembly)
            .That()
            .AreInterfaces()
            .And()
            .ResideInNamespace($"{ApplicationNs}.Abstractions")
            .Should()
            .HaveNameStartingWith("I")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "all Application interfaces should follow the I prefix convention");
    }
}
