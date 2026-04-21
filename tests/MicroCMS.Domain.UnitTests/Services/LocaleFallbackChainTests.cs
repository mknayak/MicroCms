using FluentAssertions;
using MicroCMS.Domain.Services;
using MicroCMS.Domain.ValueObjects;
using Xunit;

namespace MicroCMS.Domain.UnitTests.Services;

public sealed class LocaleFallbackChainTests
{
    private readonly LocaleFallbackChain _sut = new();
    private static readonly Locale EnUS = Locale.Create("en-US");
    private static readonly Locale En = Locale.Create("en");
    private static readonly Locale EnGB = Locale.Create("en-GB");
    private static readonly Locale FrFR = Locale.Create("fr-FR");

    [Fact]
    public void Build_ExactMatch_FirstInChain()
    {
        var enabled = new[] { EnUS, FrFR };
        var chain = _sut.Build(EnUS, EnUS, enabled);
        chain[0].Should().Be(EnUS);
    }

    [Fact]
    public void Build_RegionalVariant_FallsBackToLanguageOnly()
    {
        var enabled = new[] { En, FrFR };
        var chain = _sut.Build(EnGB, EnUS, enabled);

        // en-GB not enabled, en is → should include en
        chain.Should().Contain(En);
    }

    [Fact]
    public void Build_RequestedNotEnabled_IncludesTenantDefault()
    {
        var enabled = new[] { FrFR, EnUS };
        var chain = _sut.Build(Locale.Create("de-DE"), EnUS, enabled);
        chain.Should().Contain(EnUS);
    }

    [Fact]
    public void Build_NoDuplicates()
    {
        var enabled = new[] { EnUS, En, FrFR };
        var chain = _sut.Build(EnUS, EnUS, enabled);
        chain.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Build_AlwaysHasAtLeastOneEntry()
    {
        var enabled = new[] { FrFR };
        var chain = _sut.Build(Locale.Create("ja"), FrFR, enabled);
        chain.Should().NotBeEmpty();
    }
}
