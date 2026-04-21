using FluentAssertions;
using MicroCMS.Domain.Exceptions;
using MicroCMS.Domain.Services;
using Xunit;

namespace MicroCMS.Domain.UnitTests.Services;

public sealed class SlugGeneratorTests
{
    private readonly SlugGenerator _sut = new();

    [Theory]
    [InlineData("Hello World", "hello-world")]
    [InlineData("Hello  World", "hello-world")]     // double space → single hyphen
    [InlineData("C# & .NET", "c-net")]
    [InlineData("MicroCMS", "microcms")]
    [InlineData("Ünïcödé", "unicode")]              // diacritic normalisation
    [InlineData("café au lait", "cafe-au-lait")]
    [InlineData("Hello, World!", "hello-world")]
    public void Generate_VariousInputs_ProducesExpectedSlug(string input, string expected)
    {
        var slug = _sut.Generate(input);
        slug.Value.Should().Be(expected);
    }

    [Fact]
    public void Generate_EmptyString_ThrowsArgumentException()
    {
        var act = () => _sut.Generate(string.Empty);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Generate_OnlySpecialChars_ThrowsDomainException()
    {
        var act = () => _sut.Generate("!!! ###");
        act.Should().Throw<DomainException>().WithMessage("*no valid characters*");
    }

    [Fact]
    public void Generate_VeryLongInput_TruncatesAtWordBoundary()
    {
        var longTitle = string.Join(" ", Enumerable.Repeat("word", 60));
        var slug = _sut.Generate(longTitle);
        slug.Value.Length.Should().BeLessOrEqualTo(200);
        slug.Value.Should().NotEndWith("-");
    }

    [Fact]
    public void TryGenerate_NullInput_ReturnsNull()
    {
        var result = _sut.TryGenerate(null);
        result.Should().BeNull();
    }

    [Fact]
    public void TryGenerate_ValidInput_ReturnsSlug()
    {
        var result = _sut.TryGenerate("Hello World");
        result.Should().NotBeNull();
        result!.Value.Should().Be("hello-world");
    }
}
