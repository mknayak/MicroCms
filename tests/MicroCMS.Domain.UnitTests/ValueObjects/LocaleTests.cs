using FluentAssertions;
using MicroCMS.Domain.Exceptions;
using MicroCMS.Domain.ValueObjects;
using Xunit;

namespace MicroCMS.Domain.UnitTests.ValueObjects;

public sealed class LocaleTests
{
    [Theory]
    [InlineData("en")]
    [InlineData("en-US")]
    [InlineData("fr-FR")]
    [InlineData("zh-Hant-TW")]
    public void Create_ValidLocale_Succeeds(string value)
    {
        var locale = Locale.Create(value);
        locale.Value.Should().Be(value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1")]           // too short
    [InlineData("en_US")]       // underscore not valid
    public void Create_InvalidLocale_Throws(string value)
    {
        var act = () => Locale.Create(value);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Equality_IsCaseInsensitive()
    {
        // Locale equality normalises to lower-case
        Locale.Create("en-US").Should().Be(Locale.Create("en-us"));
    }
}
