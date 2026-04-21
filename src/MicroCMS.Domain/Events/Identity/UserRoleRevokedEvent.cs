using MicroCMS.Domain.Enums;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Events.Identity;

public sealed record UserRoleRevokedEvent(
    UserId UserId,
    TenantId TenantId,
    WorkflowRole Role) : DomainEvent;
