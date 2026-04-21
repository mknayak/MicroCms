using FluentAssertions;
using MicroCMS.Domain.Exceptions;
using MicroCMS.Domain.ValueObjects;
using Xunit;

namespace MicroCMS.Domain.UnitTests.ValueObjects;

public sealed class TenantSlugTests
{
    [Theory]
    [InlineData("acme")]
    [InlineData("acme-corp")]
    [InlineData("my-tenant-123")]
    public void Create_ValidSlug_Succeeds(string value)
    {
        var slug = TenantSlug.Create(value);
        slug.Value.Should().Be(value);
    }

    [Fact]
    public void Create_TooShort_ThrowsDomainException()
    {
        var act = () => TenantSlug.Create("ab");
        act.Should().Throw<DomainException>().WithMessage("*between*");
    }

    [Fact]
    public void Create_TooLong_ThrowsDomainException()
    {
        var act = () => TenantSlug.Create(new string('a', 64));
        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData("-start")]
    [InlineData("UPPER")]
    [InlineData("has space")]
    public void Create_InvalidFormat_ThrowsDomainException(string value)
    {
        var act = () => TenantSlug.Create(value);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Equality_SameValue_IsEqual()
    {
        TenantSlug.Create("acme").Should().Be(TenantSlug.Create("acme"));
    }
}
