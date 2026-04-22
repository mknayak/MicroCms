using FluentAssertions;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Features.Entries.Commands.DeleteEntry;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;
using NSubstitute;
using Xunit;

namespace MicroCMS.Application.UnitTests.Features.Entries.Commands;

/// <summary>
/// Unit tests for <see cref="DeleteEntryCommandHandler"/>.
/// </summary>
public sealed class DeleteEntryCommandHandlerTests
{
    private readonly IRepository<Entry, EntryId> _repository;
    private readonly DeleteEntryCommandHandler _sut;

    private readonly TenantId _tenantId = TenantId.New();
    private readonly SiteId _siteId = SiteId.New();

    public DeleteEntryCommandHandlerTests()
    {
        _repository = Substitute.For<IRepository<Entry, EntryId>>();
        _sut = new DeleteEntryCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_WhenEntryInDraftStatus_DeletesSuccessfully()
    {
        // Arrange
        var entry = Entry.Create(
            _tenantId, _siteId, ContentTypeId.New(),
            Slug.Create("deletable-entry"), Locale.English, Guid.NewGuid());

        _repository
            .GetByIdAsync(entry.Id, Arg.Any<CancellationToken>())
            .Returns(entry);

        // Act
        var result = await _sut.Handle(new DeleteEntryCommand(entry.Id.Value), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _repository.Received(1).Remove(entry);
    }

    [Fact]
    public async Task Handle_WhenEntryIsPublished_ReturnsConflictFailure()
    {
        // Arrange — bring entry to Published state
        var entry = Entry.Create(
            _tenantId, _siteId, ContentTypeId.New(),
            Slug.Create("published-entry"), Locale.English, Guid.NewGuid());

        entry.Submit();
        entry.Approve();
        entry.Publish();

        _repository
            .GetByIdAsync(entry.Id, Arg.Any<CancellationToken>())
            .Returns(entry);

        // Act
        var result = await _sut.Handle(new DeleteEntryCommand(entry.Id.Value), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Entry.CannotDeletePublished");
        _repository.DidNotReceive().Remove(Arg.Any<Entry>());
    }

    [Fact]
    public async Task Handle_WhenEntryNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _repository
            .GetByIdAsync(Arg.Any<EntryId>(), Arg.Any<CancellationToken>())
            .Returns((Entry?)null);

        // Act
        var act = async () => await _sut.Handle(new DeleteEntryCommand(Guid.NewGuid()), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
