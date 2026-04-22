using FluentAssertions;
using MicroCMS.Application.Features.Entries.Commands.Bulk;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Enums;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;
using NSubstitute;
using Xunit;

namespace MicroCMS.Application.UnitTests.Features.Entries.Commands;

/// <summary>Unit tests for bulk entry command handlers (GAP-04).</summary>
public sealed class BulkEntryCommandHandlerTests
{
    private readonly IRepository<Entry, EntryId> _repository;

    private readonly TenantId _tenantId = TenantId.New();
    private readonly SiteId _siteId = SiteId.New();

    public BulkEntryCommandHandlerTests()
    {
  _repository = Substitute.For<IRepository<Entry, EntryId>>();
    }

    // ── BulkPublish ──────────────────────────────────────────────────────

    [Fact]
    public async Task BulkPublish_WhenAllEntriesAreApproved_PublishesAllSuccessfully()
    {
        // Arrange
  var entry1 = CreateApprovedEntry("entry-1");
        var entry2 = CreateApprovedEntry("entry-2");
 SetupRepository(entry1, entry2);

   var command = new BulkPublishEntriesCommand([entry1.Id.Value, entry2.Id.Value]);
     var sut = new BulkPublishEntriesCommandHandler(_repository);

 // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
     result.IsSuccess.Should().BeTrue();
  result.Value.Succeeded.Should().HaveCount(2);
 result.Value.Failed.Should().BeEmpty();
   _repository.Received(2).Update(Arg.Any<Entry>());
    }

  [Fact]
 public async Task BulkPublish_WhenOneEntryNotFound_ReportsFailureForThatEntry()
    {
        // Arrange
     var entry = CreateApprovedEntry("found-entry");
       var missingId = Guid.NewGuid();
   _repository.GetByIdAsync(entry.Id, Arg.Any<CancellationToken>()).Returns(entry);
   _repository.GetByIdAsync(new EntryId(missingId), Arg.Any<CancellationToken>()).Returns((Entry?)null);

        var command = new BulkPublishEntriesCommand([entry.Id.Value, missingId]);
 var sut = new BulkPublishEntriesCommandHandler(_repository);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
     result.Value.Succeeded.Should().ContainSingle().Which.Should().Be(entry.Id.Value);
  result.Value.Failed.Should().ContainSingle().Which.EntryId.Should().Be(missingId);
    }

    // ── BulkDelete ───────────────────────────────────────────────────────

    [Fact]
    public async Task BulkDelete_WhenEntryIsPublished_SkipsWithFailure()
    {
  // Arrange
  var publishedEntry = CreatePublishedEntry("published");
    _repository.GetByIdAsync(publishedEntry.Id, Arg.Any<CancellationToken>()).Returns(publishedEntry);

   var command = new BulkDeleteEntriesCommand([publishedEntry.Id.Value]);
  var sut = new BulkDeleteEntriesCommandHandler(_repository);

     // Act
   var result = await sut.Handle(command, CancellationToken.None);

 // Assert
  result.Value.Succeeded.Should().BeEmpty();
  result.Value.Failed.Should().ContainSingle().Which.EntryId.Should().Be(publishedEntry.Id.Value);
  _repository.DidNotReceive().Remove(Arg.Any<Entry>());
    }

    [Fact]
    public async Task BulkDelete_WhenEntryIsDraft_DeletesSuccessfully()
    {
        // Arrange
        var draft = CreateDraftEntry("draft-to-delete");
        _repository.GetByIdAsync(draft.Id, Arg.Any<CancellationToken>()).Returns(draft);

     var command = new BulkDeleteEntriesCommand([draft.Id.Value]);
   var sut = new BulkDeleteEntriesCommandHandler(_repository);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
     result.Value.Succeeded.Should().ContainSingle().Which.Should().Be(draft.Id.Value);
     _repository.Received(1).Remove(draft);
    }

    // ── BulkUnpublish ────────────────────────────────────────────────────

    [Fact]
    public async Task BulkUnpublish_WhenEntryIsPublished_UnpublishesSuccessfully()
    {
     // Arrange
    var entry = CreatePublishedEntry("published-to-unpublish");
    _repository.GetByIdAsync(entry.Id, Arg.Any<CancellationToken>()).Returns(entry);

        var command = new BulkUnpublishEntriesCommand([entry.Id.Value]);
    var sut = new BulkUnpublishEntriesCommandHandler(_repository);

  // Act
  var result = await sut.Handle(command, CancellationToken.None);

        // Assert
       result.Value.Succeeded.Should().ContainSingle();
     result.Value.Failed.Should().BeEmpty();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private Entry CreateDraftEntry(string slug) =>
        Entry.Create(_tenantId, _siteId, ContentTypeId.New(), Slug.Create(slug), Locale.English, Guid.NewGuid());

    private Entry CreateApprovedEntry(string slug)
{
        var e = CreateDraftEntry(slug);
        e.Submit(); e.Approve();
        return e;
 }

    private Entry CreatePublishedEntry(string slug)
    {
  var e = CreateApprovedEntry(slug);
     e.Publish();
 return e;
    }

    private void SetupRepository(params Entry[] entries)
    {
    foreach (var e in entries)
  _repository.GetByIdAsync(e.Id, Arg.Any<CancellationToken>()).Returns(e);
    }
}
