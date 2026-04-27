using FluentAssertions;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Enums;
using MicroCMS.Domain.Events.Content;
using MicroCMS.Domain.Exceptions;
using MicroCMS.Domain.UnitTests.Fixtures;
using MicroCMS.Domain.ValueObjects;
using Xunit;

namespace MicroCMS.Domain.UnitTests.Aggregates;

public sealed class EntryAggregateTests
{
    // ── Creation ───────────────────────────────────────────────────────────

    [Fact]
    public void Create_ValidInputs_ReturnsEntryInDraftStatus()
    {
        var entry = CreateDraftEntry();
        entry.Status.Should().Be(EntryStatus.Draft);
        entry.CurrentVersionNumber.Should().Be(1);
    }

    [Fact]
    public void Create_RaisesEntryCreatedEvent()
    {
        var entry = CreateDraftEntry();
        entry.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<EntryCreatedEvent>();
    }

    // ── Full workflow: Draft → PendingApproval → Approved → Published ──────

    [Fact]
    public void FullWorkflow_DraftToPublished_Succeeds()
    {
        var entry = CreateDraftEntry();

        entry.Submit();
        entry.Status.Should().Be(EntryStatus.PendingReview);

        entry.Approve();
        entry.Status.Should().Be(EntryStatus.Approved);

        entry.Publish();
        entry.Status.Should().Be(EntryStatus.Published);
        entry.PublishedAt.Should().NotBeNull();
    }

    [Fact]
    public void Publish_RaisesEntryPublishedEvent()
    {
        var entry = CreateApprovedEntry();
        entry.ClearDomainEvents();
        entry.Publish();

        entry.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<EntryPublishedEvent>();
    }

    // ── Unpublish ─────────────────────────────────────────────────────────

    [Fact]
    public void Unpublish_WhenPublished_SetsStatusToUnpublished()
    {
        var entry = CreatePublishedEntry();
        entry.Unpublish();
        entry.Status.Should().Be(EntryStatus.Unpublished);
    }

    [Fact]
    public void Unpublish_WhenNotPublished_ThrowsInvalidStateTransition()
    {
        var entry = CreateDraftEntry();
        var act = () => entry.Unpublish();
        act.Should().Throw<InvalidStateTransitionException>();
    }

    // ── Publish guards ────────────────────────────────────────────────────

    [Fact]
    public void Publish_WhenDraft_ThrowsBusinessRuleViolation()
    {
        var entry = CreateDraftEntry();
        var act = () => entry.Publish();
        act.Should().Throw<BusinessRuleViolationException>()
            .WithMessage("*CannotPublishWithoutApproval*");
    }

    // ── UpdateFields ──────────────────────────────────────────────────────

    [Fact]
    public void UpdateFields_IncrementsVersionNumber()
    {
        var entry = CreateDraftEntry();
        var initialVersion = entry.CurrentVersionNumber;

        entry.UpdateFields("{\"title\":\"Updated\"}", Guid.NewGuid(), "Fixed typo");

        entry.CurrentVersionNumber.Should().Be(initialVersion + 1);
        entry.Versions.Should().HaveCount(initialVersion + 1);
    }

    [Fact]
    public void UpdateFields_WhenPublished_ThrowsBusinessRuleViolation()
    {
        var entry = CreatePublishedEntry();
        var act = () => entry.UpdateFields("{}", Guid.NewGuid());
        act.Should().Throw<BusinessRuleViolationException>().WithMessage("*NotEditable*");
    }

    // ── ReturnToDraft ─────────────────────────────────────────────────────

    [Fact]
    public void ReturnToDraft_WhenPendingApproval_ResetsToDraft()
    {
        var entry = CreateDraftEntry();
        entry.Submit();
        entry.ReturnToDraft("Needs more detail");
        entry.Status.Should().Be(EntryStatus.Draft);
    }

    // ── Scheduling ────────────────────────────────────────────────────────

    [Fact]
    public void SchedulePublish_WhenApproved_SetsScheduledStatus()
    {
        var entry = CreateApprovedEntry();
        var publishAt = DateTimeOffset.UtcNow.AddHours(2);

        entry.SchedulePublish(publishAt);

        entry.Status.Should().Be(EntryStatus.Scheduled);
        entry.ScheduledPublishAt.Should().Be(publishAt);
    }

    [Fact]
    public void SchedulePublish_WithPastDate_ThrowsDomainException()
    {
        var entry = CreateApprovedEntry();
        var act = () => entry.SchedulePublish(DateTimeOffset.UtcNow.AddMinutes(-1));
        act.Should().Throw<Exceptions.DomainException>().WithMessage("*future*");
    }

    [Fact]
    public void SchedulePublish_UnpublishBeforePublish_ThrowsDomainException()
    {
        var entry = CreateApprovedEntry();
        var publishAt = DateTimeOffset.UtcNow.AddHours(2);
        var act = () => entry.SchedulePublish(publishAt, unpublishAt: publishAt.AddMinutes(-1));
        act.Should().Throw<Exceptions.DomainException>().WithMessage("*after publish*");
    }

    // ── Rollback ──────────────────────────────────────────────────────────

    [Fact]
    public void RollbackToVersion_ValidVersion_RestoresFieldsJson()
    {
        var entry = CreateDraftEntry();
        var originalJson = entry.FieldsJson;
        var authorId = Guid.NewGuid();

        entry.UpdateFields("{\"title\":\"v2\"}", authorId);
        entry.RollbackToVersion(1, authorId);

        entry.FieldsJson.Should().Be(originalJson);
        entry.CurrentVersionNumber.Should().Be(3); // create, update, rollback
    }

    [Fact]
    public void RollbackToVersion_InvalidVersion_ThrowsDomainException()
    {
        var entry = CreateDraftEntry();
        var act = () => entry.RollbackToVersion(99, Guid.NewGuid());
        act.Should().Throw<Exceptions.DomainException>().WithMessage("*not found*");
    }

    // ── Archive ───────────────────────────────────────────────────────────

    [Fact]
    public void Archive_SetsStatusToArchived()
    {
        var entry = CreateDraftEntry();
        entry.Archive();
        entry.Status.Should().Be(EntryStatus.Archived);
    }

    [Fact]
    public void Archive_WhenAlreadyArchived_ThrowsBusinessRuleViolation()
    {
        var entry = CreateDraftEntry();
        entry.Archive();
        var act = () => entry.Archive();
        act.Should().Throw<BusinessRuleViolationException>().WithMessage("*AlreadyArchived*");
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static Entry CreateDraftEntry() =>
        Entry.Create(
            DomainFixtures.TenantId,
            DomainFixtures.SiteId,
            DomainFixtures.ContentTypeId,
            DomainFixtures.EntrySlug,
            DomainFixtures.DefaultLocale,
            DomainFixtures.UserId.Value);

    private static Entry CreateApprovedEntry()
    {
        var entry = CreateDraftEntry();
        entry.Submit();
        entry.Approve();
        entry.ClearDomainEvents();
        return entry;
    }

    private static Entry CreatePublishedEntry()
    {
        var entry = CreateApprovedEntry();
        entry.Publish();
        entry.ClearDomainEvents();
        return entry;
    }
}
