using FluentAssertions;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.Media.Commands;
using MicroCMS.Application.Features.Media.Handlers;
using MicroCMS.Domain.Aggregates.Media;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Media;
using MicroCMS.Shared.Ids;
using NSubstitute;
using Xunit;

namespace MicroCMS.Application.UnitTests.Features.Media;

public sealed class MediaFolderCommandHandlerTests
{
    private readonly IRepository<MediaFolder, Guid> _folderRepo = Substitute.For<IRepository<MediaFolder, Guid>>();
    private readonly IRepository<MediaAsset, MediaAssetId> _assetRepo = Substitute.For<IRepository<MediaAsset, MediaAssetId>>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly TenantId _tenantId = TenantId.New();
    private readonly Guid _siteId = Guid.NewGuid();

    public MediaFolderCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(_tenantId);
    }

    [Fact]
    public async Task CreateMediaFolder_ShouldReturnDto_WithCorrectName()
    {
        var command = new CreateMediaFolderCommand(_siteId, "My Uploads");
        var sut = new CreateMediaFolderCommandHandler(_folderRepo, _currentUser);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("My Uploads");
        await _folderRepo.Received(1).AddAsync(Arg.Any<MediaFolder>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteMediaFolder_ShouldFail_WhenFolderHasAssets()
    {
        // Arrange
        var folderId = Guid.NewGuid();
        var folder = MediaFolderFactory.Create(_tenantId, new SiteId(_siteId), "Not Empty", null);
        _folderRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(folder);
        _folderRepo.ListAsync(Arg.Any<ChildMediaFoldersSpec>(), Arg.Any<CancellationToken>())
            .Returns(new List<MediaFolder>());

        // Simulate one asset in the folder
        var metadata = MicroCMS.Domain.ValueObjects.AssetMetadata.Create("file.jpg", "image/jpeg", 1024);
        var asset = MediaAsset.Create(_tenantId, new SiteId(_siteId), metadata, "key", Guid.NewGuid());
        asset.MarkUploadComplete();
        asset.MarkAvailable();
        _assetRepo.ListAsync(Arg.Any<MediaAssetsByFolderSpec>(), Arg.Any<CancellationToken>())
            .Returns(new List<MediaAsset> { asset });

        var command = new DeleteMediaFolderCommand(folderId);
        var sut = new DeleteMediaFolderCommandHandler(_folderRepo, _assetRepo);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("MediaFolder.NotEmpty");
        _folderRepo.DidNotReceive().Remove(Arg.Any<MediaFolder>());
    }

    [Fact]
    public async Task DeleteMediaFolder_ShouldSucceed_WhenFolderIsEmpty()
    {
        var folderId = Guid.NewGuid();
        var folder = MediaFolderFactory.Create(_tenantId, new SiteId(_siteId), "Empty Folder", null);
        _folderRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(folder);
        _folderRepo.ListAsync(Arg.Any<ChildMediaFoldersSpec>(), Arg.Any<CancellationToken>())
            .Returns(new List<MediaFolder>());
        _assetRepo.ListAsync(Arg.Any<MediaAssetsByFolderSpec>(), Arg.Any<CancellationToken>())
            .Returns(new List<MediaAsset>());

        var command = new DeleteMediaFolderCommand(folderId);
        var sut = new DeleteMediaFolderCommandHandler(_folderRepo, _assetRepo);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _folderRepo.Received(1).Remove(folder);
    }

    [Fact]
    public async Task RenameMediaFolder_ShouldUpdateName()
    {
        var folder = MediaFolderFactory.Create(_tenantId, new SiteId(_siteId), "Old Name", null);
        _folderRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(folder);

        var command = new RenameMediaFolderCommand(folder.Id, "New Name");
        var sut = new RenameMediaFolderCommandHandler(_folderRepo);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task RenameMediaFolder_ShouldThrow_WhenFolderNotFound()
    {
        _folderRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((MediaFolder?)null);

        var command = new RenameMediaFolderCommand(Guid.NewGuid(), "New Name");
        var sut = new RenameMediaFolderCommandHandler(_folderRepo);

        var act = async () => await sut.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
