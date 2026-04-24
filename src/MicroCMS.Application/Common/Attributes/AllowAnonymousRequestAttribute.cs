namespace MicroCMS.Application.Common.Attributes;

/// <summary>
/// Marks a MediatR command or query as explicitly anonymous — i.e., it may be dispatched
/// without a valid authenticated user and without a <see cref="HasPolicyAttribute"/>.
///
/// Use this sparingly for requests that must work before authentication is possible:
/// - First-run install
/// - Login / token refresh
/// - Password reset initiation
///
/// <see cref="Behaviors.AuthorizationBehavior{TRequest,TResponse}"/> skips all policy
/// and authentication checks when this attribute is present.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class AllowAnonymousRequestAttribute : Attribute;
