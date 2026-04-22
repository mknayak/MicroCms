using FluentAssertions;
using MediatR;
using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Behaviors;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Shared.Results;
using NSubstitute;
using Xunit;

namespace MicroCMS.Application.UnitTests.Behaviors;

/// <summary>
/// Unit tests for <see cref="AuthorizationBehavior{TRequest,TResponse}"/>.
/// </summary>
public sealed class AuthorizationBehaviorTests
{
    private readonly ICurrentUser _currentUser;
    private readonly IApplicationAuthorizationService _authService;

    public AuthorizationBehaviorTests()
    {
        _currentUser = Substitute.For<ICurrentUser>();
        _authService = Substitute.For<IApplicationAuthorizationService>();
    }

    [Fact]
    public async Task Handle_WhenPolicyGranted_CallsNext()
    {
        // Arrange
        _currentUser.IsAuthenticated.Returns(true);
        _authService.IsAuthorized(Arg.Any<string[]>()).Returns(true);

        var behavior = new AuthorizationBehavior<AuthorizedRequest, Result<string>>(_currentUser, _authService);
        var nextCalled = false;
        RequestHandlerDelegate<Result<string>> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Success("done"));
        };

        // Act
        var result = await behavior.Handle(new AuthorizedRequest(), next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ThrowsUnauthorizedException()
    {
        // Arrange
        _currentUser.IsAuthenticated.Returns(false);

        var behavior = new AuthorizationBehavior<AuthorizedRequest, Result<string>>(_currentUser, _authService);
        RequestHandlerDelegate<Result<string>> next = () => Task.FromResult(Result.Success("done"));

        // Act
        var act = async () => await behavior.Handle(new AuthorizedRequest(), next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_WhenAuthenticatedButPolicyDenied_ThrowsForbiddenException()
    {
        // Arrange
        _currentUser.IsAuthenticated.Returns(true);
        _authService.IsAuthorized(Arg.Any<string[]>()).Returns(false);

        var behavior = new AuthorizationBehavior<AuthorizedRequest, Result<string>>(_currentUser, _authService);
        RequestHandlerDelegate<Result<string>> next = () => Task.FromResult(Result.Success("done"));

        // Act
        var act = async () => await behavior.Handle(new AuthorizedRequest(), next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .Where(ex => ex.Policy == ContentPolicies.EntryCreate);
    }

    [Fact]
    public async Task Handle_WhenRequestMissingPolicyAttribute_ThrowsMissingPolicyException()
    {
        // Arrange
        _currentUser.IsAuthenticated.Returns(true);

        var behavior = new AuthorizationBehavior<UnannotatedRequest, Result<string>>(_currentUser, _authService);
        RequestHandlerDelegate<Result<string>> next = () => Task.FromResult(Result.Success("done"));

        // Act
        var act = async () => await behavior.Handle(new UnannotatedRequest(), next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<MissingPolicyException>()
            .Where(ex => ex.RequestType == typeof(UnannotatedRequest));
    }

    // ── Test stubs ───────────────────────────────────────────────────────────

    [HasPolicy(ContentPolicies.EntryCreate)]
    private sealed record AuthorizedRequest : ICommand<string>;

    // Intentionally NOT decorated with [HasPolicy]
    private sealed record UnannotatedRequest : ICommand<string>;
}
