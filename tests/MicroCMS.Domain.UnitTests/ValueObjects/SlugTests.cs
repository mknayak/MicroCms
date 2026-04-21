using FluentAssertions;
using MicroCMS.Domain.Exceptions;
using MicroCMS.Domain.ValueObjects;
using Xunit;

namespace MicroCMS.Domain.UnitTests.ValueObjects;

public sealed class SlugTests
{
    [Theory]
    [InlineData("my-post")]
    [InlineData("hello-world")]
    [InlineData("a")]
    [InlineData("123")]
    [InlineData("abc-123-def")]
    public void Create_ValidSlug_Succeeds(string value)
    {
        var slug = Slug.Create(value);
        slug.Value.Should().Be(value);
    }

    [Theory]
    [InlineData("My Post")]       // uppercase and space
    [InlineData("-start")]        // leading hyphen
    [InlineData("end-")]          // trailing hyphen
    [InlineData("has--double")]   // consecutive hyphens
    [InlineData("has_underscore")]// underscore not allowed
    [InlineData("")]
    [InlineData("   ")]
    public void Create_InvalidSlug_ThrowsDomainException(string value)
    {
        var act = () => Slug.Create(value);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Create_ExceedsMaxLength_ThrowsDomainException()
    {
        var longValue = string.Concat(Enumerable.Repeat("a", Slug.MaxLength + 1));
        var act = () => Slug.Create(longValue);
        act.Should().Throw<DomainException>().WithMessage("*must not exceed*");
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var a = Slug.Create("hello-world");
        var b = Slug.Create("hello-world");
        a.Should().Be(b);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = Slug.Create("hello-world");
        var b = Slug.Create("goodbye-world");
        a.Should().NotBe(b);
    }
}
