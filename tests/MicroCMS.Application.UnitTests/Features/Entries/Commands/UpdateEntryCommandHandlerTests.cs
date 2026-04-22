using FluentAssertions;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.Entries.Commands.UpdateEntry;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;
using NSubstitute;
using Xunit;

namespace MicroCMS.Application.UnitTests.Features.Entries.Commands;

/// <summary>
/// Unit tests for <see cref="UpdateEntryCommandHandler"/>.
/// </summary>
public sealed class UpdateEntryCommandHandlerTests
{
    private readonly IRepository<Entry, EntryId> _repository;
    private readonly ICurrentUser _currentUser;
    private readonly UpdateEntryCommandHandler _sut;

    private readonly TenantId _tenantId = TenantId.New();
    private readonly SiteId _siteId = SiteId.New();
    private readonly Entry _existingEntry;

    public UpdateEntryCommandHandlerTests()
    {
        _repository = Substitute.For<IRepository<Entry, EntryId>>();
        _currentUser = Substitute.For<ICurrentUser>();

        _currentUser.TenantId.Returns(_tenantId);
        _currentUser.UserId.Returns(Guid.NewGuid());

        _existingEntry = Entry.Create(
            _tenantId, _siteId, ContentTypeId.New(),
            Slug.Create("original-slug"),
            Locale.English,
            Guid.NewGuid());

        _repository
            .GetByIdAsync(_existingEntry.Id, Arg.Any<CancellationToken>())
            .Returns(_existingEntry);

        // No slug conflicts by default
        _repository
            .ListAsync(Arg.Any<ISpecification<Entry>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Entry>());

        _sut = new UpdateEntryCommandHandler(_repository, _currentUser);
    }

    [Fact]
    public async Task Handle_WithValidUpdate_UpdatesFieldsAndReturnsDto()
    {
        // Arrange
        var command = new UpdateEntryCommand(
            EntryId: _existingEntry.Id.Value,
            FieldsJson: """{"title":"Updated"}""",
            ChangeNote: "Fixed typo");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FieldsJson.Should().Be("""{"title":"Updated"}""");
        _repository.Received(1).Update(_existingEntry);
    }

    [Fact]
    public async Task Handle_WhenEntryNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _repository
            .GetByIdAsync(Arg.Any<EntryId>(), Arg.Any<CancellationToken>())
            .Returns((Entry?)null);

        var command = new UpdateEntryCommand(
            EntryId: Guid.NewGuid(),
            FieldsJson: "{}");

        // Act
        var act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WithNewSlugThatConflicts_ReturnsConflictFailure()
    {
        // Arrange
        var conflicting = Entry.Create(
            _tenantId, _siteId, ContentTypeId.New(),
            Slug.Create("taken-slug"), Locale.English, Guid.NewGuid());

        _repository
            .ListAsync(Arg.Any<ISpecification<Entry>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { conflicting });

        var command = new UpdateEntryCommand(
            EntryId: _existingEntry.Id.Value,
            FieldsJson: "{}",
            NewSlug: "taken-slug");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Entry.SlugConflict");
    }

    [Fact]
    public async Task Handle_WithNewSlugUnique_UpdatesSlug()
    {
        // Arrange
        var command = new UpdateEntryCommand(
            EntryId: _existingEntry.Id.Value,
            FieldsJson: "{}",
            NewSlug: "new-unique-slug");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Slug.Should().Be("new-unique-slug");
    }
}
