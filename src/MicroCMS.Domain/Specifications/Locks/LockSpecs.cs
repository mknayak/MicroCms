using MicroCMS.Domain.Aggregates.Locks;
using MicroCMS.Domain.Specifications;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Specifications.Locks;

public sealed class LockByEntityIdSpec : BaseSpecification<EditLock>
{
    public LockByEntityIdSpec(string entityId)
        : base(l => l.EntityId == entityId) { }
}
