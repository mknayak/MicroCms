using FluentAssertions;
using MicroCMS.Domain.Aggregates.Identity;
using MicroCMS.Domain.Enums;
using MicroCMS.Domain.Events.Identity;
using MicroCMS.Domain.Exceptions;
using MicroCMS.Domain.UnitTests.Fixtures;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;
using Xunit;

namespace MicroCMS.Domain.UnitTests.Aggregates;

public sealed class UserAggregateTests
{
    [Fact]
    public void Create_ValidInputs_ReturnsActiveUser()
    {
        var user = CreateUser();
        user.IsActive.Should().BeTrue();
        user.Roles.Should().BeEmpty();
    }

    [Fact]
    public void Create_RaisesUserCreatedEvent()
    {
        var user = CreateUser();
        user.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserCreatedEvent>();
    }

    // ── Roles ─────────────────────────────────────────────────────────────

    [Fact]
    public void AssignRole_ValidRole_AddsToRoles()
    {
        var user = CreateUser();
        user.AssignRole(WorkflowRole.Author, "Author");
        user.Roles.Should().HaveCount(1);
    }

    [Fact]
    public void AssignRole_RaisesUserRoleAssignedEvent()
    {
        var user = CreateUser();
        user.ClearDomainEvents();
        user.AssignRole(WorkflowRole.Author, "Author");

        user.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserRoleAssignedEvent>();
    }

    [Fact]
    public void AssignRole_DuplicateTenantWideRole_ThrowsBusinessRuleViolation()
    {
        var user = CreateUser();
        user.AssignRole(WorkflowRole.Author, "Author");
        var act = () => user.AssignRole(WorkflowRole.Author, "Author");
        act.Should().Throw<BusinessRuleViolationException>().WithMessage("*RoleAlreadyAssigned*");
    }

    [Fact]
    public void RevokeRole_ExistingRole_RemovesIt()
    {
        var user = CreateUser();
        var role = user.AssignRole(WorkflowRole.Publisher, "Publisher");
        user.RevokeRole(role.Id);
        user.Roles.Should().BeEmpty();
    }

    [Fact]
    public void RevokeRole_NonExistentRole_ThrowsDomainException()
    {
        var user = CreateUser();
        var act = () => user.RevokeRole(RoleId.New());
        act.Should().Throw<DomainException>();
    }

    // ── HasRole helper ────────────────────────────────────────────────────

    [Fact]
    public void HasRole_WithMatchingTenantWideRole_ReturnsTrue()
    {
        var user = CreateUser();
        user.AssignRole(WorkflowRole.Editor, "Editor");
        user.HasRole(WorkflowRole.Editor).Should().BeTrue();
    }

    [Fact]
    public void HasRole_WithoutRole_ReturnsFalse()
    {
        var user = CreateUser();
        user.HasRole(WorkflowRole.Publisher).Should().BeFalse();
    }

    // ── Deactivation ──────────────────────────────────────────────────────

    [Fact]
    public void Deactivate_ActiveUser_SetsInactive()
    {
        var user = CreateUser();
        user.Deactivate();
        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_AlreadyInactive_ThrowsBusinessRuleViolation()
    {
        var user = CreateUser();
        user.Deactivate();
        var act = () => user.Deactivate();
        act.Should().Throw<BusinessRuleViolationException>().WithMessage("*AlreadyInactive*");
    }

    [Fact]
    public void AssignRole_ToInactiveUser_ThrowsBusinessRuleViolation()
    {
        var user = CreateUser();
        user.Deactivate();
        var act = () => user.AssignRole(WorkflowRole.Author, "Author");
        act.Should().Throw<BusinessRuleViolationException>().WithMessage("*Inactive*");
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static User CreateUser() =>
        User.Create(
            DomainFixtures.TenantId,
            DomainFixtures.ValidEmail,
            DomainFixtures.ValidName);
}
