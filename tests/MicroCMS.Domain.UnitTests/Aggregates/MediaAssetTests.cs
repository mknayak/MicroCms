using FluentAssertions;
using MicroCMS.Domain.Aggregates.Media;
using MicroCMS.Domain.Enums;
using MicroCMS.Domain.Events.Media;
using MicroCMS.Domain.Exceptions;
using MicroCMS.Domain.UnitTests.Fixtures;
using MicroCMS.Domain.ValueObjects;
using Xunit;

namespace MicroCMS.Domain.UnitTests.Aggregates;

public sealed class MediaAssetTests
{
    [Fact]
    public void Create_ValidInputs_StatusIsUploading()
    {
        var asset = CreateAsset();
        asset.Status.Should().Be(MediaAssetStatus.Uploading);
    }

    [Fact]
    public void Create_RaisesUploadStartedEvent()
    {
        var asset = CreateAsset();
        asset.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<MediaAssetUploadStartedEvent>();
    }

    [Fact]
    public void MarkUploadComplete_TransitionsToPendingScan()
    {
        var asset = CreateAsset();
        asset.MarkUploadComplete();
        asset.Status.Should().Be(MediaAssetStatus.PendingScan);
    }

    [Fact]
    public void MarkAvailable_FromPendingScan_SetsAvailable()
    {
        var asset = CreateAsset();
        asset.MarkUploadComplete();
        asset.MarkAvailable();
        asset.Status.Should().Be(MediaAssetStatus.Available);
    }

    [Fact]
    public void MarkAvailable_RaisesMediaAssetAvailableEvent()
    {
        var asset = CreateAsset();
        asset.MarkUploadComplete();
        asset.ClearDomainEvents();
        asset.MarkAvailable();

        asset.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<MediaAssetAvailableEvent>();
    }

    [Fact]
    public void Quarantine_FromPendingScan_SetsQuarantined()
    {
        var asset = CreateAsset();
        asset.MarkUploadComplete();
        asset.Quarantine("Virus detected: EICAR");
        asset.Status.Should().Be(MediaAssetStatus.Quarantined);
    }

    [Fact]
    public void MarkAvailable_WhenUploading_ThrowsInvalidStateTransition()
    {
        var asset = CreateAsset();
        var act = () => asset.MarkAvailable();
        act.Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void UpdateAltText_WhenAvailable_Succeeds()
    {
        var asset = CreateAvailableAsset();
        asset.UpdateAltText("A beautiful sunset");
        asset.AltText.Should().Be("A beautiful sunset");
    }

    [Fact]
    public void UpdateAltText_WhenQuarantined_ThrowsBusinessRuleViolation()
    {
        var asset = CreateAsset();
        asset.MarkUploadComplete();
        asset.Quarantine("Infected");
        var act = () => asset.UpdateAltText("alt");
        act.Should().Throw<BusinessRuleViolationException>().WithMessage("*NotAvailable*");
    }

    [Fact]
    public void SetTags_NormalisesToLowerCaseAndDeduplicates()
    {
        var asset = CreateAvailableAsset();
        asset.SetTags(["Photo", "photo", "LANDSCAPE"]);
        asset.Tags.Should().BeEquivalentTo(["photo", "landscape"]);
    }

    [Fact]
    public void Delete_SetsStatusToDeleted()
    {
        var asset = CreateAvailableAsset();
        asset.Delete(Guid.NewGuid());
        asset.Status.Should().Be(MediaAssetStatus.Deleted);
    }

    [Fact]
    public void Delete_WhenAlreadyDeleted_ThrowsBusinessRuleViolation()
    {
        var asset = CreateAvailableAsset();
        asset.Delete(Guid.NewGuid());
        var act = () => asset.Delete(Guid.NewGuid());
        act.Should().Throw<BusinessRuleViolationException>().WithMessage("*AlreadyDeleted*");
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static MediaAsset CreateAsset()
    {
        var metadata = AssetMetadata.Create("photo.jpg", "image/jpeg", 1024 * 1024);
        return MediaAsset.Create(
            DomainFixtures.TenantId,
            DomainFixtures.SiteId,
            metadata,
            "uploads/photo.jpg",
            Guid.NewGuid());
    }

    private static MediaAsset CreateAvailableAsset()
    {
        var asset = CreateAsset();
        asset.MarkUploadComplete();
        asset.MarkAvailable();
        asset.ClearDomainEvents();
        return asset;
    }
}
