using FluentAssertions;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Features.Entries.Commands.Workflow;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Enums;
using MicroCMS.Domain.Exceptions;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;
using NSubstitute;
using Xunit;

namespace MicroCMS.Application.UnitTests.Features.Entries.Commands;

/// <summary>
/// Unit tests for approval workflow command handlers (GAP-07).
/// Covers SubmitForReview, ApproveEntry, and RejectEntry.
/// </summary>
public sealed class WorkflowCommandHandlerTests
{
    private readonly IRepository<Entry, EntryId> _repository;

    private readonly TenantId _tenantId = TenantId.New();
    private readonly SiteId _siteId = SiteId.New();

    public WorkflowCommandHandlerTests()
    {
        _repository = Substitute.For<IRepository<Entry, EntryId>>();
    }

    // ── SubmitForReview ──────────────────────────────────────────────────

    [Fact]
    public async Task SubmitForReview_WhenEntryIsDraft_TransitionsToPendingApproval()
    {
        // Arrange
  var entry = CreateDraftEntry();
        _repository.GetByIdAsync(entry.Id, Arg.Any<CancellationToken>()).Returns(entry);
        var sut = new SubmitForReviewCommandHandler(_repository);

     // Act
        var result = await sut.Handle(new SubmitForReviewCommand(entry.Id.Value), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(EntryStatus.PendingApproval.ToString());
        _repository.Received(1).Update(entry);
    }

 [Fact]
    public async Task SubmitForReview_WhenEntryNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _repository.GetByIdAsync(Arg.Any<EntryId>(), Arg.Any<CancellationToken>()).Returns((Entry?)null);
        var sut = new SubmitForReviewCommandHandler(_repository);

// Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
   () => sut.Handle(new SubmitForReviewCommand(Guid.NewGuid()), CancellationToken.None));
    }

    // ── ApproveEntry ─────────────────────────────────────────────────────

    [Fact]
 public async Task ApproveEntry_WhenEntryIsPendingApproval_TransitionsToApproved()
    {
        // Arrange
    var entry = CreatePendingApprovalEntry();
 _repository.GetByIdAsync(entry.Id, Arg.Any<CancellationToken>()).Returns(entry);
    var sut = new ApproveEntryCommandHandler(_repository);

        // Act
  var result = await sut.Handle(new ApproveEntryCommand(entry.Id.Value), CancellationToken.None);

        // Assert
 result.IsSuccess.Should().BeTrue();
  result.Value.Status.Should().Be(EntryStatus.Approved.ToString());
    }

    [Fact]
    public async Task ApproveEntry_WhenEntryIsDraft_ThrowsDomainException()
    {
        // Arrange — cannot approve a Draft (must be PendingApproval)
        var entry = CreateDraftEntry();
        _repository.GetByIdAsync(entry.Id, Arg.Any<CancellationToken>()).Returns(entry);
        var sut = new ApproveEntryCommandHandler(_repository);

        // Act & Assert
    await Assert.ThrowsAsync<InvalidStateTransitionException>(
   () => sut.Handle(new ApproveEntryCommand(entry.Id.Value), CancellationToken.None));
    }

    // ── RejectEntry ───────────────────────────────────────────────────────

    [Fact]
  public async Task RejectEntry_WhenEntryIsPendingApproval_ReturnsEntryToDraft()
    {
   // Arrange
      var entry = CreatePendingApprovalEntry();
   _repository.GetByIdAsync(entry.Id, Arg.Any<CancellationToken>()).Returns(entry);
     var sut = new RejectEntryCommandHandler(_repository);

   // Act
        var result = await sut.Handle(new RejectEntryCommand(entry.Id.Value, "Grammar issues."), CancellationToken.None);

     // Assert
        result.IsSuccess.Should().BeTrue();
    result.Value.Status.Should().Be(EntryStatus.Draft.ToString());
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private Entry CreateDraftEntry() =>
  Entry.Create(_tenantId, _siteId, ContentTypeId.New(),
   Slug.Create("workflow-entry"), Locale.English, Guid.NewGuid());

    private Entry CreatePendingApprovalEntry()
    {
        var entry = CreateDraftEntry();
        entry.Submit();
      return entry;
    }
}
