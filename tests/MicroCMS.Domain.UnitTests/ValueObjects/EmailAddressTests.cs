using FluentAssertions;
using MicroCMS.Domain.ValueObjects;
using Xunit;

namespace MicroCMS.Domain.UnitTests.ValueObjects;

public sealed class EmailAddressTests
{
    [Theory]
    [InlineData("alice@example.com")]
    [InlineData("ALICE@EXAMPLE.COM")]   // normalised to lower case
    [InlineData("user+tag@domain.org")]
    public void Create_ValidEmail_Succeeds(string value)
    {
        var email = EmailAddress.Create(value);
        email.Value.Should().Be(value.Trim().ToLowerInvariant());
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("@nodomain")]
    [InlineData("nolocalpart@")]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_InvalidEmail_Throws(string value)
    {
        var act = () => EmailAddress.Create(value);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Equality_IsCaseInsensitive()
    {
        EmailAddress.Create("Alice@Example.com").Should().Be(EmailAddress.Create("alice@example.com"));
    }
}
