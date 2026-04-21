using FluentAssertions;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Enums;
using MicroCMS.Domain.Events.Content;
using MicroCMS.Domain.Exceptions;
using MicroCMS.Domain.UnitTests.Fixtures;
using Xunit;

namespace MicroCMS.Domain.UnitTests.Aggregates;

public sealed class ContentTypeTests
{
    [Fact]
    public void Create_ValidInputs_CreatesInDraftStatus()
    {
        var ct = CreateContentType();
        ct.Status.Should().Be(ContentTypeStatus.Draft);
        ct.Fields.Should().BeEmpty();
    }

    [Fact]
    public void Create_RaisesContentTypeCreatedEvent()
    {
        var ct = CreateContentType();
        ct.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ContentTypeCreatedEvent>();
    }

    [Theory]
    [InlineData("InvalidHandle-has-hyphens")]
    [InlineData("has space")]
    [InlineData("")]
    public void Create_InvalidHandle_Throws(string handle)
    {
        var act = () => ContentType.Create(
            DomainFixtures.TenantId,
            DomainFixtures.SiteId,
            handle,
            "Display Name");
        act.Should().Throw<Exception>();
    }

    // ── Fields ────────────────────────────────────────────────────────────

    [Fact]
    public void AddField_ValidField_AppendsToFields()
    {
        var ct = CreateContentType();
        ct.AddField("title", "Title", FieldType.ShortText, isRequired: true);
        ct.Fields.Should().HaveCount(1);
        ct.Fields[0].Handle.Should().Be("title");
    }

    [Fact]
    public void AddField_DuplicateHandle_ThrowsBusinessRuleViolation()
    {
        var ct = CreateContentType();
        ct.AddField("title", "Title", FieldType.ShortText);
        var act = () => ct.AddField("title", "Another Title", FieldType.LongText);
        act.Should().Throw<BusinessRuleViolationException>().WithMessage("*DuplicateFieldHandle*");
    }

    [Fact]
    public void RemoveField_ExistingField_RemovesIt()
    {
        var ct = CreateContentType();
        var field = ct.AddField("body", "Body", FieldType.RichText);
        ct.RemoveField(field.Id);
        ct.Fields.Should().BeEmpty();
    }

    [Fact]
    public void RemoveField_NonExistentField_ThrowsDomainException()
    {
        var ct = CreateContentType();
        var act = () => ct.RemoveField(Guid.NewGuid());
        act.Should().Throw<DomainException>();
    }

    // ── Publish / Archive ─────────────────────────────────────────────────

    [Fact]
    public void Publish_WithAtLeastOneField_SetsStatusToActive()
    {
        var ct = CreateContentType();
        ct.AddField("title", "Title", FieldType.ShortText);
        ct.Publish();
        ct.Status.Should().Be(ContentTypeStatus.Active);
    }

    [Fact]
    public void Publish_WithNoFields_ThrowsBusinessRuleViolation()
    {
        var ct = CreateContentType();
        var act = () => ct.Publish();
        act.Should().Throw<BusinessRuleViolationException>().WithMessage("*NoFields*");
    }

    [Fact]
    public void Archive_SetsStatusToArchived()
    {
        var ct = CreateContentType();
        ct.Archive();
        ct.Status.Should().Be(ContentTypeStatus.Archived);
    }

    [Fact]
    public void AddField_WhenArchived_ThrowsBusinessRuleViolation()
    {
        var ct = CreateContentType();
        ct.Archive();
        var act = () => ct.AddField("title", "Title", FieldType.ShortText);
        act.Should().Throw<BusinessRuleViolationException>().WithMessage("*IsArchived*");
    }

    private static ContentType CreateContentType() =>
        ContentType.Create(
            DomainFixtures.TenantId,
            DomainFixtures.SiteId,
            "blog_post",
            "Blog Post");
}
