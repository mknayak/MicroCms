using FluentAssertions;
using MicroCMS.Application.Common.Authorization;
using Xunit;

namespace MicroCMS.Application.UnitTests.Authorization;

/// <summary>
/// Tests that the Approver role (GAP-07) has the correct policy grants
/// and that new policies (EntryReview, EntryExport, FolderManage) are
/// correctly distributed across all roles.
/// </summary>
public sealed class RolePermissionsExtendedTests
{
    // ── Approver role ────────────────────────────────────────────────────

    [Fact]
    public void Approver_CanReviewEntries()
    {
  RolePermissions.IsGranted([Roles.Approver], ContentPolicies.EntryReview).Should().BeTrue();
    }

    [Fact]
    public void Approver_CanReadEntriesAndMedia()
    {
  RolePermissions.IsGranted([Roles.Approver], ContentPolicies.EntryRead).Should().BeTrue();
 RolePermissions.IsGranted([Roles.Approver], ContentPolicies.MediaRead).Should().BeTrue();
    }

    [Fact]
    public void Approver_CannotPublishOrDelete()
    {
  RolePermissions.IsGranted([Roles.Approver], ContentPolicies.EntryPublish).Should().BeFalse();
 RolePermissions.IsGranted([Roles.Approver], ContentPolicies.EntryDelete).Should().BeFalse();
    }

    // ── EntryReview policy ───────────────────────────────────────────────

    [Theory]
    [InlineData(Roles.SystemAdmin)]
    [InlineData(Roles.TenantAdmin)]
    [InlineData(Roles.Approver)]
    public void EntryReview_IsGrantedTo(string role)
    {
    RolePermissions.IsGranted([role], ContentPolicies.EntryReview).Should().BeTrue();
    }

    [Theory]
    [InlineData(Roles.Author)]
    [InlineData(Roles.Viewer)]
    public void EntryReview_IsNotGrantedTo(string role)
    {
        RolePermissions.IsGranted([role], ContentPolicies.EntryReview).Should().BeFalse();
    }

    // ── EntryExport policy ───────────────────────────────────────────────

    [Theory]
    [InlineData(Roles.SystemAdmin)]
    [InlineData(Roles.TenantAdmin)]
    [InlineData(Roles.Editor)]
    public void EntryExport_IsGrantedToEditorAndAbove(string role)
    {
        RolePermissions.IsGranted([role], ContentPolicies.EntryExport).Should().BeTrue();
    }

    [Theory]
    [InlineData(Roles.Author)]
    [InlineData(Roles.Viewer)]
 public void EntryExport_IsNotGrantedToAuthorOrViewer(string role)
    {
        RolePermissions.IsGranted([role], ContentPolicies.EntryExport).Should().BeFalse();
    }

    // ── FolderManage policy ──────────────────────────────────────────────

    [Theory]
    [InlineData(Roles.SystemAdmin)]
[InlineData(Roles.TenantAdmin)]
    [InlineData(Roles.Editor)]
 public void FolderManage_IsGrantedToEditorAndAbove(string role)
    {
        RolePermissions.IsGranted([role], ContentPolicies.FolderManage).Should().BeTrue();
    }

    [Theory]
[InlineData(Roles.Author)]
  [InlineData(Roles.Viewer)]
    public void FolderManage_IsNotGrantedToAuthorOrViewer(string role)
    {
        RolePermissions.IsGranted([role], ContentPolicies.FolderManage).Should().BeFalse();
    }
}
