using FluentAssertions;
using MicroCMS.Application.Common.Authorization;
using Xunit;

namespace MicroCMS.Application.UnitTests.Authorization;

/// <summary>
/// Tests for <see cref="RolePermissions"/> role-to-policy mapping.
/// </summary>
public sealed class RolePermissionsTests
{
    [Theory]
    [InlineData(Roles.SystemAdmin, ContentPolicies.EntryCreate)]
    [InlineData(Roles.SystemAdmin, ContentPolicies.EntryDelete)]
    [InlineData(Roles.SystemAdmin, ContentPolicies.SystemAdmin)]
    [InlineData(Roles.TenantAdmin, ContentPolicies.EntryPublish)]
    [InlineData(Roles.TenantAdmin, ContentPolicies.UserManage)]
    [InlineData(Roles.Editor, ContentPolicies.EntryCreate)]
    [InlineData(Roles.Editor, ContentPolicies.EntryPublish)]
    [InlineData(Roles.Author, ContentPolicies.EntryCreate)]
    [InlineData(Roles.Viewer, ContentPolicies.EntryRead)]
    public void IsGranted_WhenRoleHasPolicy_ReturnsTrue(string role, string policy)
    {
        var result = RolePermissions.IsGranted([role], policy);
        result.Should().BeTrue($"role '{role}' should grant policy '{policy}'");
    }

    [Theory]
    [InlineData(Roles.Author, ContentPolicies.EntryDelete)]
    [InlineData(Roles.Author, ContentPolicies.EntryPublish)]
    [InlineData(Roles.Viewer, ContentPolicies.EntryCreate)]
    [InlineData(Roles.Viewer, ContentPolicies.EntryDelete)]
    [InlineData(Roles.Editor, ContentPolicies.SystemAdmin)]
    [InlineData(Roles.Editor, ContentPolicies.TenantManage)]
    public void IsGranted_WhenRoleDoesNotHavePolicy_ReturnsFalse(string role, string policy)
    {
        var result = RolePermissions.IsGranted([role], policy);
        result.Should().BeFalse($"role '{role}' should NOT grant policy '{policy}'");
    }

    [Fact]
    public void IsGranted_WhenEmptyRoles_ReturnsFalse()
    {
        var result = RolePermissions.IsGranted([], ContentPolicies.EntryRead);
        result.Should().BeFalse("no roles means no grants");
    }

    [Fact]
    public void IsGranted_WhenMultipleRoles_UnionGranted()
    {
        // Viewer + Editor: Editor adds publish access that Viewer alone lacks
        var result = RolePermissions.IsGranted(
            [Roles.Viewer, Roles.Editor],
            ContentPolicies.EntryPublish);

        result.Should().BeTrue("the union of Viewer and Editor includes EntryPublish");
    }

    [Fact]
    public void IsGranted_IsCaseInsensitiveForRoles()
    {
        var result = RolePermissions.IsGranted(["sysTemAdmIn"], ContentPolicies.SystemAdmin);
        result.Should().BeTrue("role matching is case-insensitive");
    }
}
