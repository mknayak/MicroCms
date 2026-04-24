using System.Linq.Expressions;
using MicroCMS.Domain.Aggregates.Identity;
using MicroCMS.Domain.Specifications;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Specifications.Identity;

/// <summary>All users for the current tenant (filter applied by global query filter).</summary>
public sealed class AllUsersPagedSpec : BaseSpecification<User>
{
    public AllUsersPagedSpec(int page, int pageSize)
        : base(_ => true)
    {
     ApplyOrderBy(u => u.Email);
        ApplyPaging((page - 1) * pageSize, pageSize);
    }
}

/// <summary>Count-only — no paging.</summary>
public sealed class AllUsersCountSpec : BaseSpecification<User>
{
    public AllUsersCountSpec() : base(_ => true) { }
}

/// <summary>Looks up a user by email within the tenant scope.</summary>
public sealed class UserByEmailSpec : BaseSpecification<User>
{
    public UserByEmailSpec(string email)
        : base(BuildCriteria(email)) { }

    private static Expression<Func<User, bool>> BuildCriteria(string email)
    {
        var normalised = EmailAddress.Create(email);
        return u => u.Email == normalised;
    }
}
