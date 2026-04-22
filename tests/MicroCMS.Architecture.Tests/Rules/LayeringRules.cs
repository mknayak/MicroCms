using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace MicroCMS.Architecture.Tests.Rules;

/// <summary>
/// Enforces the clean-architecture layering rules defined in §7.1 of the design doc.
/// These tests run in CI and gate every PR.
/// </summary>
public sealed class LayeringRules
{
    private static readonly string[] DomainAssemblies = ["MicroCMS.Domain"];
    private static readonly string[] ApplicationAssemblies = ["MicroCMS.Application"];
    private static readonly string[] InfrastructureAssemblies = ["MicroCMS.Infrastructure"];
    private static readonly string[] PresentationAssemblies = ["MicroCMS.Api", "MicroCMS.GraphQL"];

    [Fact]
    public void Domain_ShouldNot_DependOn_Application()
    {
        var result = Types.InAssembly(typeof(MicroCMS.Domain.Aggregates.AggregateRoot<object>).Assembly)
            .ShouldNot()
            .HaveDependencyOn("MicroCMS.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Domain must not reference Application (clean architecture).");
    }

    [Fact]
    public void Domain_ShouldNot_DependOn_Infrastructure()
    {
        var result = Types.InAssembly(typeof(MicroCMS.Domain.Aggregates.AggregateRoot<object>).Assembly)
            .ShouldNot()
            .HaveDependencyOn("MicroCMS.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Domain must not reference Infrastructure.");
    }

    [Fact]
    public void Application_ShouldNot_DependOn_Infrastructure()
    {
        var result = Types.InAssembly(typeof(MicroCMS.Application.Common.Interfaces.IUnitOfWork).Assembly)
            .ShouldNot()
            .HaveDependencyOn("MicroCMS.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Application must not reference Infrastructure (dependency inversion).");
    }

    [Fact]
    public void Presentation_ShouldNot_DependOn_Infrastructure()
    {
        var apiAssembly = typeof(MicroCMS.Api.AssemblyReference).Assembly;

        var result = Types.InAssembly(apiAssembly)
            .ShouldNot()
            .HaveDependencyOn("MicroCMS.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "API controllers must not depend on Infrastructure directly.");
    }

    [Fact]
    public void AiProviders_ShouldNot_DependOn_Application()
    {
        var result = Types
            .InAssembly(typeof(MicroCMS.Ai.Providers.AzureOpenAI.AssemblyReference).Assembly)
            .ShouldNot()
            .HaveDependencyOn("MicroCMS.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "AI provider adapters may only reference Ai.Abstractions and Shared.");
    }
}
