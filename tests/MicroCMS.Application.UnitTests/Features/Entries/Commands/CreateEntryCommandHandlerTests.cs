using FluentAssertions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.Entries.Commands.CreateEntry;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications;
using MicroCMS.Shared.Ids;
using NSubstitute;
using Xunit;

namespace MicroCMS.Application.UnitTests.Features.Entries.Commands;

/// <summary>
/// Unit tests for <see cref="CreateEntryCommandHandler"/>.
/// </summary>
public sealed class CreateEntryCommandHandlerTests
{
    private readonly IRepository<Entry, EntryId> _repository;
    private readonly ICurrentUser _currentUser;
    private readonly CreateEntryCommandHandler _sut;

    private readonly TenantId _tenantId = TenantId.New();
    private readonly SiteId _siteId = SiteId.New();

    public CreateEntryCommandHandlerTests()
    {
        _repository = Substitute.For<IRepository<Entry, EntryId>>();
        _currentUser = Substitute.For<ICurrentUser>();

        _currentUser.TenantId.Returns(_tenantId);
        _currentUser.UserId.Returns(Guid.NewGuid());
        _currentUser.IsAuthenticated.Returns(true);

        // No slug conflicts by default
        _repository
            .ListAsync(Arg.Any<ISpecification<Entry>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Entry>());

        _sut = new CreateEntryCommandHandler(_repository, _currentUser);
    }

    [Fact]
    public async Task Handle_WithValidCommand_CreatesEntryAndReturnsDto()
    {
        // Arrange
        var command = new CreateEntryCommand(
            SiteId: _siteId.Value,
            ContentTypeId: ContentTypeId.New().Value,
            Slug: "my-article",
            Locale: "en",
            FieldsJson: """{"title":"Hello"}""");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Slug.Should().Be("my-article");
        result.Value.Locale.Should().Be("en");
        result.Value.TenantId.Should().Be(_tenantId.Value);
        await _repository.Received(1).AddAsync(Arg.Any<Entry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSlugAlreadyExists_ReturnsConflictFailure()
    {
        // Arrange
        var existingSiteId = _siteId;
        var existingEntry = Entry.Create(
            _tenantId, existingSiteId, ContentTypeId.New(),
            MicroCMS.Domain.ValueObjects.Slug.Create("taken-slug"),
            MicroCMS.Domain.ValueObjects.Locale.English,
            Guid.NewGuid());

        _repository
            .ListAsync(Arg.Any<ISpecification<Entry>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { existingEntry });

        var command = new CreateEntryCommand(
            SiteId: existingSiteId.Value,
            ContentTypeId: ContentTypeId.New().Value,
            Slug: "taken-slug",
            Locale: "en");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Entry.SlugConflict");
        await _repository.DidNotReceive().AddAsync(Arg.Any<Entry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithDefaultFieldsJson_SetsEmptyObject()
    {
        // Arrange
        var command = new CreateEntryCommand(
            SiteId: _siteId.Value,
            ContentTypeId: ContentTypeId.New().Value,
            Slug: "defaults-entry",
            Locale: "en-US");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Fields.Should().BeNullOrEmpty();
    }
}
