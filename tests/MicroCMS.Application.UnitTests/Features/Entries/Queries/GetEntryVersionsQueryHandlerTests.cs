using FluentAssertions;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Features.Entries.Queries.GetEntryVersions;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;
using NSubstitute;
using Xunit;

namespace MicroCMS.Application.UnitTests.Features.Entries.Queries;

/// <summary>
/// Unit tests for <see cref="GetEntryVersionsQueryHandler"/>.
/// </summary>
public sealed class GetEntryVersionsQueryHandlerTests
{
    private readonly IRepository<Entry, EntryId> _repository;
    private readonly GetEntryVersionsQueryHandler _sut;

    private readonly TenantId _tenantId = TenantId.New();
    private readonly SiteId _siteId = SiteId.New();

    public GetEntryVersionsQueryHandlerTests()
    {
        _repository = Substitute.For<IRepository<Entry, EntryId>>();
        _sut = new GetEntryVersionsQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_WhenEntryExists_ReturnsVersionsOrderedNewestFirst()
    {
        // Arrange — create entry with two versions (initial + one update)
        var entry = Entry.Create(
            _tenantId, _siteId, ContentTypeId.New(),
            Slug.Create("versioned-entry"), Locale.English, Guid.NewGuid(),
            """{"v":1}""");

        entry.UpdateFields("""{"v":2}""", Guid.NewGuid(), "Second version");

        _repository
            .GetByIdAsync(entry.Id, Arg.Any<CancellationToken>())
            .Returns(entry);

        var query = new GetEntryVersionsQuery(entry.Id.Value);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().BeInDescendingOrder(v => v.VersionNumber,
            "versions are returned newest-first");
        result.Value.First().VersionNumber.Should().Be(2);
        result.Value.Last().VersionNumber.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenEntryNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _repository
            .GetByIdAsync(Arg.Any<EntryId>(), Arg.Any<CancellationToken>())
            .Returns((Entry?)null);

        var query = new GetEntryVersionsQuery(Guid.NewGuid());

        // Act
        var act = async () => await _sut.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
