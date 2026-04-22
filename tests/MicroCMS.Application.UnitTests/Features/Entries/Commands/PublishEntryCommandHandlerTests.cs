using FluentAssertions;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Features.Entries.Commands.PublishEntry;
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
/// Unit tests for <see cref="PublishEntryCommandHandler"/>.
/// </summary>
public sealed class PublishEntryCommandHandlerTests
{
    private readonly IRepository<Entry, EntryId> _repository;
    private readonly PublishEntryCommandHandler _sut;

    private readonly TenantId _tenantId = TenantId.New();
    private readonly SiteId _siteId = SiteId.New();

    public PublishEntryCommandHandlerTests()
    {
        _repository = Substitute.For<IRepository<Entry, EntryId>>();
        _sut = new PublishEntryCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_WhenEntryIsApproved_PublishesSuccessfully()
    {
        // Arrange — create entry and move to Approved status
        var entry = CreateApprovedEntry();

        _repository
            .GetByIdAsync(entry.Id, Arg.Any<CancellationToken>())
            .Returns(entry);

        var command = new PublishEntryCommand(entry.Id.Value);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(EntryStatus.Published.ToString());
        _repository.Received(1).Update(entry);
    }

    [Fact]
    public async Task Handle_WhenEntryNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _repository
            .GetByIdAsync(Arg.Any<EntryId>(), Arg.Any<CancellationToken>())
            .Returns((Entry?)null);

        // Act
        var act = async () => await _sut.Handle(new PublishEntryCommand(Guid.NewGuid()), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenEntryInDraftStatus_ThrowsDomainException()
    {
        // Arrange — Draft entry cannot be published without approval
        var draftEntry = Entry.Create(
            _tenantId, _siteId, ContentTypeId.New(),
            Slug.Create("draft-entry"), Locale.English, Guid.NewGuid());

        _repository
            .GetByIdAsync(draftEntry.Id, Arg.Any<CancellationToken>())
            .Returns(draftEntry);

        // Act
        var act = async () => await _sut.Handle(new PublishEntryCommand(draftEntry.Id.Value), CancellationToken.None);

        // Assert — Entry.Publish() throws a domain exception for non-Approved entries
        await act.Should().ThrowAsync<BusinessRuleViolationException>();
    }

    private Entry CreateApprovedEntry()
    {
        var entry = Entry.Create(
            _tenantId, _siteId, ContentTypeId.New(),
            Slug.Create("approved-entry"), Locale.English, Guid.NewGuid());

        entry.Submit();
        entry.Approve();

        return entry;
    }
}
