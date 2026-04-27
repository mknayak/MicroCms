using FluentAssertions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.Entries.Queries.ListEntries;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;
using NSubstitute;
using Xunit;

namespace MicroCMS.Application.UnitTests.Features.Entries.Queries;

/// <summary>
/// Unit tests for <see cref="ListEntriesQueryHandler"/>.
/// </summary>
public sealed class ListEntriesQueryHandlerTests
{
    private readonly IRepository<Entry, EntryId> _repository;
    private readonly ICacheService _cache;
    private readonly ICurrentUser _currentUser;
  private readonly ListEntriesQueryHandler _sut;

    private readonly TenantId _tenantId = TenantId.New();
    private readonly SiteId _siteId = SiteId.New();

    public ListEntriesQueryHandlerTests()
    {
        _repository = Substitute.For<IRepository<Entry, EntryId>>();
        _cache = Substitute.For<ICacheService>();
        _currentUser = Substitute.For<ICurrentUser>();
        _currentUser.TenantId.Returns(_tenantId);

    // Cache always misses in unit tests.
        _cache.GetAsync<object>(Arg.Any<string>(), Arg.Any<CancellationToken>())
.Returns((object?)null);

        _sut = new ListEntriesQueryHandler(_repository, _cache, _currentUser);
    }

    [Fact]
    public async Task Handle_WithEntries_ReturnsMappedPagedList()
    {
        // Arrange
        var entry1 = Entry.Create(_tenantId, _siteId, ContentTypeId.New(), Slug.Create("entry-1"), Locale.English, Guid.NewGuid());
        var entry2 = Entry.Create(_tenantId, _siteId, ContentTypeId.New(), Slug.Create("entry-2"), Locale.English, Guid.NewGuid());

        _repository
            .ListAsync(Arg.Any<ISpecification<Entry>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { entry1, entry2 });

        _repository
            .CountAsync(Arg.Any<ISpecification<Entry>>(), Arg.Any<CancellationToken>())
            .Returns(2);

        var query = new ListEntriesQuery(SiteId: _siteId.Value, PageNumber: 1, PageSize: 10);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task Handle_WhenNoEntries_ReturnsEmptyPagedList()
    {
        // Arrange
        _repository
            .ListAsync(Arg.Any<ISpecification<Entry>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Entry>());

        _repository
            .CountAsync(Arg.Any<ISpecification<Entry>>(), Arg.Any<CancellationToken>())
            .Returns(0);

        var query = new ListEntriesQuery(SiteId: _siteId.Value);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
        result.Value.HasNextPage.Should().BeFalse();
        result.Value.HasPreviousPage.Should().BeFalse();
    }
}
