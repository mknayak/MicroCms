using FluentAssertions;
using MicroCMS.Application.Features.Entries.Commands.CreateEntry;
using Xunit;

namespace MicroCMS.Application.UnitTests.Features.Entries.Validators;

/// <summary>
/// Unit tests for <see cref="CreateEntryCommandValidator"/>.
/// </summary>
public sealed class CreateEntryCommandValidatorTests
{
    private readonly CreateEntryCommandValidator _sut = new();

    [Fact]
    public async Task Validate_WithValidCommand_PassesValidation()
    {
        var command = new CreateEntryCommand(
            SiteId: Guid.NewGuid(),
            ContentTypeId: Guid.NewGuid(),
            Slug: "valid-slug",
            Locale: "en-US",
            FieldsJson: """{"title":"Hello"}""");

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("INVALID SLUG")]
    [InlineData("-starts-with-hyphen")]
    [InlineData("ends-with-hyphen-")]
    [InlineData("contains_underscore")]
    [InlineData("CamelCase")]
    public async Task Validate_WithInvalidSlug_FailsValidation(string slug)
    {
        var command = new CreateEntryCommand(
            SiteId: Guid.NewGuid(),
            ContentTypeId: Guid.NewGuid(),
            Slug: slug,
            Locale: "en");

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateEntryCommand.Slug));
    }

    [Theory]
    [InlineData("en")]
    [InlineData("en-US")]
    [InlineData("zh-Hant-TW")]
    public async Task Validate_WithValidLocale_PassesValidation(string locale)
    {
        var command = new CreateEntryCommand(
            SiteId: Guid.NewGuid(),
            ContentTypeId: Guid.NewGuid(),
            Slug: "my-article",
            Locale: locale);

        var result = await _sut.ValidateAsync(command);

        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateEntryCommand.Locale));
    }

    [Fact]
    public async Task Validate_WithInvalidJson_FailsValidation()
    {
        var command = new CreateEntryCommand(
            SiteId: Guid.NewGuid(),
            ContentTypeId: Guid.NewGuid(),
            Slug: "my-article",
            Locale: "en",
            FieldsJson: "not-valid-json{");

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateEntryCommand.FieldsJson));
    }

    [Fact]
    public async Task Validate_WithEmptySiteId_FailsValidation()
    {
        var command = new CreateEntryCommand(
            SiteId: Guid.Empty,
            ContentTypeId: Guid.NewGuid(),
            Slug: "my-article",
            Locale: "en");

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateEntryCommand.SiteId));
    }
}
