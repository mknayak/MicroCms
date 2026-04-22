using FluentAssertions;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Features.Entries.Commands.UpdateSeoMetadata;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;
using NSubstitute;
using Xunit;

namespace MicroCMS.Application.UnitTests.Features.Entries.Commands;

/// <summary>Unit tests for <see cref="UpdateSeoMetadataCommandHandler"/> (GAP-08).</summary>
public sealed class UpdateSeoMetadataCommandHandlerTests
{
    private readonly IRepository<Entry, EntryId> _repository;
    private readonly UpdateSeoMetadataCommandHandler _sut;

    private readonly TenantId _tenantId = TenantId.New();
    private readonly SiteId _siteId = SiteId.New();

    public UpdateSeoMetadataCommandHandlerTests()
    {
        _repository = Substitute.For<IRepository<Entry, EntryId>>();
        _sut = new UpdateSeoMetadataCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_WithValidSeoFields_UpdatesEntryAndReturnsDto()
    {
        // Arrange
  var entry = CreateDraftEntry();
        _repository.GetByIdAsync(entry.Id, Arg.Any<CancellationToken>()).Returns(entry);

        var command = new UpdateSeoMetadataCommand(
         EntryId: entry.Id.Value,
    MetaTitle: "Short title",
      MetaDescription: "A helpful description under 160 chars.",
    CanonicalUrl: "https://example.com/my-article");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
   result.IsSuccess.Should().BeTrue();
    result.Value.Seo!.MetaTitle.Should().Be("Short title");
        result.Value.Seo.MetaDescription.Should().Be("A helpful description under 160 chars.");
     result.Value.Seo.CanonicalUrl.Should().Be("https://example.com/my-article");
        _repository.Received(1).Update(entry);
    }

    [Fact]
  public async Task Handle_WhenEntryNotFound_ThrowsNotFoundException()
    {
     // Arrange
        _repository.GetByIdAsync(Arg.Any<EntryId>(), Arg.Any<CancellationToken>()).Returns((Entry?)null);

 // Act
   var act = async () => await _sut.Handle(
    new UpdateSeoMetadataCommand(Guid.NewGuid(), null, null), CancellationToken.None);

 // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WithNullSeoFields_ClearsSeoMetadata()
    {
        // Arrange
     var entry = CreateDraftEntry();
    _repository.GetByIdAsync(entry.Id, Arg.Any<CancellationToken>()).Returns(entry);

    // Act
var result = await _sut.Handle(
    new UpdateSeoMetadataCommand(entry.Id.Value, null, null), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
      result.Value.Seo!.MetaTitle.Should().BeNull();
        result.Value.Seo.MetaDescription.Should().BeNull();
    }

    private Entry CreateDraftEntry() =>
    Entry.Create(_tenantId, _siteId, ContentTypeId.New(),
   Slug.Create("seo-entry"), Locale.English, Guid.NewGuid());
}
