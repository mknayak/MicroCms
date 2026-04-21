using MicroCMS.Domain.Enums;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Events.Identity;

public sealed record UserRoleAssignedEvent(
    UserId UserId,
    TenantId TenantId,
    WorkflowRole Role,
    SiteId? SiteId) : DomainEvent;
