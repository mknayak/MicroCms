using FluentAssertions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.Media.Commands;
using MicroCMS.Application.Features.Media.Handlers;
using MicroCMS.Domain.Aggregates.Media;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;
using NSubstitute;
using Xunit;

namespace MicroCMS.Application.UnitTests.Features.Media;

public sealed class BulkMediaCommandHandlerTests
{
    private readonly IRepository<MediaAsset, MediaAssetId> _repo = Substitute.For<IRepository<MediaAsset, MediaAssetId>>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly TenantId _tenantId = TenantId.New();

    public BulkMediaCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(_tenantId);
        _currentUser.UserId.Returns(Guid.NewGuid());
    }

    // ── BulkMove ─────────────────────────────────────────────────────────

    [Fact]
    public async Task BulkMove_ShouldMoveAllAssetsToTargetFolder()
    {
        // Arrange
        var targetFolder = Guid.NewGuid();
        var asset1 = CreateAvailableAsset();
        var asset2 = CreateAvailableAsset();

        _repo.GetByIdAsync(asset1.Id, Arg.Any<CancellationToken>()).Returns(asset1);
        _repo.GetByIdAsync(asset2.Id, Arg.Any<CancellationToken>()).Returns(asset2);

        var command = new BulkMoveMediaCommand([asset1.Id.Value, asset2.Id.Value], targetFolder);
        var sut = new BulkMoveMediaCommandHandler(_repo, _currentUser);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        asset1.FolderId.Should().Be(targetFolder);
        asset2.FolderId.Should().Be(targetFolder);
        _repo.Received(2).Update(Arg.Any<MediaAsset>());
    }

    [Fact]
    public async Task BulkMove_ShouldSetFolderToNull_WhenTargetIsNull()
    {
        var asset = CreateAvailableAsset(existingFolder: Guid.NewGuid());
        _repo.GetByIdAsync(asset.Id, Arg.Any<CancellationToken>()).Returns(asset);

        var command = new BulkMoveMediaCommand([asset.Id.Value], TargetFolderId: null);
        var sut = new BulkMoveMediaCommandHandler(_repo, _currentUser);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        asset.FolderId.Should().BeNull();
    }

    // ── BulkDelete ───────────────────────────────────────────────────────

    [Fact]
    public async Task BulkDelete_ShouldSoftDeleteAllAssets()
    {
        var asset1 = CreateAvailableAsset();
        var asset2 = CreateAvailableAsset();

        _repo.GetByIdAsync(asset1.Id, Arg.Any<CancellationToken>()).Returns(asset1);
        _repo.GetByIdAsync(asset2.Id, Arg.Any<CancellationToken>()).Returns(asset2);

        var command = new BulkDeleteMediaCommand([asset1.Id.Value, asset2.Id.Value]);
        var sut = new BulkDeleteMediaCommandHandler(_repo, _currentUser);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        asset1.Status.Should().Be(MicroCMS.Domain.Enums.MediaAssetStatus.Deleted);
        asset2.Status.Should().Be(MicroCMS.Domain.Enums.MediaAssetStatus.Deleted);
    }

    // ── BulkRetag ────────────────────────────────────────────────────────

    [Fact]
    public async Task BulkRetag_ShouldReplaceTagsOnAllAssets()
    {
        var asset1 = CreateAvailableAsset();
        var asset2 = CreateAvailableAsset();

        _repo.GetByIdAsync(asset1.Id, Arg.Any<CancellationToken>()).Returns(asset1);
        _repo.GetByIdAsync(asset2.Id, Arg.Any<CancellationToken>()).Returns(asset2);

        var newTags = new List<string> { "hero", "banner" };
        var command = new BulkRetagMediaCommand([asset1.Id.Value, asset2.Id.Value], newTags);
        var sut = new BulkRetagMediaCommandHandler(_repo, _currentUser);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        asset1.Tags.Should().BeEquivalentTo(newTags);
        asset2.Tags.Should().BeEquivalentTo(newTags);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private MediaAsset CreateAvailableAsset(Guid? existingFolder = null)
    {
        var metadata = AssetMetadata.Create("test.jpg", "image/jpeg", 1024);
        var asset = MediaAsset.Create(
            _tenantId,
            SiteId.New(),
            metadata,
            $"storage-key-{Guid.NewGuid():N}",
            _currentUser.UserId,
            existingFolder);

        asset.MarkUploadComplete();   // Uploading → PendingScan
        asset.MarkAvailable();        // PendingScan → Available
        return asset;
    }
}
