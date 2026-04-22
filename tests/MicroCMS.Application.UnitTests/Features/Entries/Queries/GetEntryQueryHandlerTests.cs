using FluentAssertions;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Features.Entries.Queries.GetEntry;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;
using NSubstitute;
using Xunit;

namespace MicroCMS.Application.UnitTests.Features.Entries.Queries;

/// <summary>
/// Unit tests for <see cref="GetEntryQueryHandler"/>.
/// </summary>
public sealed class GetEntryQueryHandlerTests
{
    private readonly IRepository<Entry, EntryId> _repository;
    private readonly GetEntryQueryHandler _sut;

    private readonly TenantId _tenantId = TenantId.New();
    private readonly SiteId _siteId = SiteId.New();

    public GetEntryQueryHandlerTests()
    {
        _repository = Substitute.For<IRepository<Entry, EntryId>>();
        _sut = new GetEntryQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_WhenEntryExists_ReturnsMappedDto()
    {
        // Arrange
        var entry = Entry.Create(
            _tenantId, _siteId, ContentTypeId.New(),
            Slug.Create("my-entry"), Locale.English, Guid.NewGuid(),
            """{"title":"Hello"}""");

        _repository
            .GetByIdAsync(entry.Id, Arg.Any<CancellationToken>())
            .Returns(entry);

        var query = new GetEntryQuery(entry.Id.Value);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(entry.Id.Value);
        result.Value.Slug.Should().Be("my-entry");
        result.Value.Locale.Should().Be("en");
        result.Value.TenantId.Should().Be(_tenantId.Value);
        result.Value.FieldsJson.Should().Be("""{"title":"Hello"}""");
    }

    [Fact]
    public async Task Handle_WhenEntryNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _repository
            .GetByIdAsync(Arg.Any<EntryId>(), Arg.Any<CancellationToken>())
            .Returns((Entry?)null);

        var query = new GetEntryQuery(Guid.NewGuid());

        // Act
        var act = async () => await _sut.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Entry*");
    }
}
