using FluentAssertions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.Media.Commands;
using MicroCMS.Application.Features.Media.Handlers;
using MicroCMS.Domain.Aggregates.Media;
using MicroCMS.Domain.Enums;
using MicroCMS.Domain.Repositories;
using MicroCMS.Shared.Ids;
using NSubstitute;
using Xunit;

namespace MicroCMS.Application.UnitTests.Features.Media;

public sealed class UploadMediaAssetCommandHandlerTests
{
    private readonly IStorageProvider _storage = Substitute.For<IStorageProvider>();
    private readonly IMimeTypeInspector _mimeInspector = Substitute.For<IMimeTypeInspector>();
    private readonly IRepository<MediaAsset, MediaAssetId> _repo = Substitute.For<IRepository<MediaAsset, MediaAssetId>>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private readonly TenantId _tenantId = TenantId.New();
    private readonly Guid _siteId = Guid.NewGuid();

    public UploadMediaAssetCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(_tenantId);
        _currentUser.UserId.Returns(Guid.NewGuid());
        _mimeInspector.DetectAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("image/jpeg");
        _storage.UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("tenant/2026/04/abc123_photo.jpg");
    }

    [Fact]
    public async Task Handle_ShouldCreateAssetWithPendingScanStatus()
    {
        // Arrange
        var command = new UploadMediaAssetCommand(
            _siteId,
            "photo.jpg",
            new MemoryStream(new byte[1024]),
            1024,
            "image/jpeg");

        var sut = new UploadMediaAssetCommandHandler(_storage, _mimeInspector, _repo, _currentUser);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(MediaAssetStatus.PendingScan.ToString());
        result.Value.FileName.Should().Be("photo.jpg");
        await _repo.Received(1).AddAsync(Arg.Any<MediaAsset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldUseDetectedMimeType_NotClientProvided()
    {
        // Arrange — client claims PNG but magic bytes say JPEG
        _mimeInspector.DetectAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("image/jpeg");

        var command = new UploadMediaAssetCommand(
            _siteId, "photo.jpg", new MemoryStream(new byte[512]), 512, "image/png");

        var sut = new UploadMediaAssetCommandHandler(_storage, _mimeInspector, _repo, _currentUser);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert — the stored MIME is what the inspector detected
        result.IsSuccess.Should().BeTrue();
        result.Value.MimeType.Should().Be("image/jpeg");
    }

    [Fact]
    public async Task Handle_ShouldFailValidation_WhenFileTooLarge()
    {
        // Arrange
        var overLimit = MicroCMS.Domain.ValueObjects.AssetMetadata.MaxFileSizeBytes + 1;
        var command = new UploadMediaAssetCommand(
            _siteId, "huge.bin", Stream.Null, overLimit, "application/octet-stream");

        var sut = new UploadMediaAssetCommandHandler(_storage, _mimeInspector, _repo, _currentUser);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Media.FileTooLarge");
        await _repo.DidNotReceive().AddAsync(Arg.Any<MediaAsset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldCallStorageProvider_WithTenantId()
    {
        var command = new UploadMediaAssetCommand(
            _siteId, "doc.pdf", new MemoryStream(new byte[256]), 256, "application/pdf");

        var sut = new UploadMediaAssetCommandHandler(_storage, _mimeInspector, _repo, _currentUser);
        await sut.Handle(command, CancellationToken.None);

        await _storage.Received(1).UploadAsync(
            Arg.Any<Stream>(),
            "doc.pdf",
            Arg.Any<string>(),
            _tenantId.Value.ToString(),
            Arg.Any<CancellationToken>());
    }
}
