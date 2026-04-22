using FluentAssertions;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Features.Entries.Commands.CancelScheduledPublish;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Enums;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;
using NSubstitute;
using Xunit;

namespace MicroCMS.Application.UnitTests.Features.Entries.Commands;

/// <summary>Unit tests for <see cref="CancelScheduledPublishCommandHandler"/> (GAP-06).</summary>
public sealed class CancelScheduledPublishCommandHandlerTests
{
    private readonly IRepository<Entry, EntryId> _repository;
    private readonly CancelScheduledPublishCommandHandler _sut;

    private readonly TenantId _tenantId = TenantId.New();
private readonly SiteId _siteId = SiteId.New();

    public CancelScheduledPublishCommandHandlerTests()
    {
        _repository = Substitute.For<IRepository<Entry, EntryId>>();
        _sut = new CancelScheduledPublishCommandHandler(_repository);
    }

    [Fact]
 public async Task Handle_WhenEntryIsScheduled_CancelsAndReturnsApproved()
    {
     // Arrange
        var entry = CreateScheduledEntry();
  _repository.GetByIdAsync(entry.Id, Arg.Any<CancellationToken>()).Returns(entry);

 // Act
 var result = await _sut.Handle(new CancelScheduledPublishCommand(entry.Id.Value), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
      result.Value.Status.Should().Be(EntryStatus.Approved.ToString());
 result.Value.ScheduledPublishAt.Should().BeNull();
  result.Value.ScheduledUnpublishAt.Should().BeNull();
   _repository.Received(1).Update(entry);
 }

    [Fact]
 public async Task Handle_WhenEntryNotFound_ThrowsNotFoundException()
    {
      // Arrange
 _repository.GetByIdAsync(Arg.Any<EntryId>(), Arg.Any<CancellationToken>()).Returns((Entry?)null);

     // Act
        var act = async () => await _sut.Handle(new CancelScheduledPublishCommand(Guid.NewGuid()), CancellationToken.None);

 // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenEntryIsNotScheduled_ThrowsDomainException()
    {
        // Arrange — Draft entry is not Scheduled
        var draftEntry = Entry.Create(_tenantId, _siteId, ContentTypeId.New(),
      Slug.Create("draft"), Locale.English, Guid.NewGuid());
      _repository.GetByIdAsync(draftEntry.Id, Arg.Any<CancellationToken>()).Returns(draftEntry);

        // Act
        var act = async () => await _sut.Handle(new CancelScheduledPublishCommand(draftEntry.Id.Value), CancellationToken.None);

        // Assert — CancelScheduledPublish() throws for non-Scheduled entries
        await act.Should().ThrowAsync<Exception>();
    }

    private Entry CreateScheduledEntry()
    {
        var entry = Entry.Create(_tenantId, _siteId, ContentTypeId.New(),
     Slug.Create("scheduled"), Locale.English, Guid.NewGuid());
    entry.Submit();
  entry.Approve();
        entry.SchedulePublish(DateTimeOffset.UtcNow.AddDays(1));
        return entry;
    }
}
