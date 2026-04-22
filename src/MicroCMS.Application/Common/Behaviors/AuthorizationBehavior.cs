using MediatR;
using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;

namespace MicroCMS.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that enforces declarative authorization before any handler runs.
///
/// Pipeline position: after <see cref="LoggingBehavior{TRequest,TResponse}"/>,
/// before <see cref="ValidationBehavior{TRequest,TResponse}"/>.
///
/// Security contract:
/// 1. If the request type has no <see cref="HasPolicyAttribute"/>, <see cref="MissingPolicyException"/>
///    is thrown — every command/query must explicitly declare its required policy.
/// 2. If the caller is not authenticated, <see cref="UnauthorizedException"/> is thrown (HTTP 401).
/// 3. If authenticated but missing any required policy, <see cref="ForbiddenException"/> is thrown (HTTP 403).
/// </summary>
public sealed class AuthorizationBehavior<TRequest, TResponse>(
    ICurrentUser currentUser,
    IApplicationAuthorizationService authorizationService)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var policies = GetDeclaredPolicies(typeof(TRequest));

        // Fail-secure: every request type must declare at least one policy.
        if (policies.Length == 0)
        {
            throw new MissingPolicyException(typeof(TRequest));
        }

        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException();
        }

        foreach (var policy in policies)
        {
            if (!authorizationService.IsAuthorized(policy))
            {
                throw new ForbiddenException(policy);
            }
        }

        return next();
    }

    private static string[] GetDeclaredPolicies(Type requestType) =>
        requestType
            .GetCustomAttributes(typeof(HasPolicyAttribute), inherit: true)
            .Cast<HasPolicyAttribute>()
            .Select(a => a.Policy)
            .ToArray();
}
