using FluentAssertions;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Interfaces;
using NSubstitute;
using Xunit;

namespace MicroCMS.Application.UnitTests.Authorization;

/// <summary>
/// Tests for <see cref="DefaultApplicationAuthorizationService"/>.
/// </summary>
public sealed class DefaultApplicationAuthorizationServiceTests
{
    private readonly ICurrentUser _currentUser;
    private readonly DefaultApplicationAuthorizationService _sut;

    public DefaultApplicationAuthorizationServiceTests()
    {
        _currentUser = Substitute.For<ICurrentUser>();
        _sut = new DefaultApplicationAuthorizationService(_currentUser);
    }

    [Fact]
    public void IsAuthorized_WhenUnauthenticated_ReturnsFalse()
    {
        _currentUser.IsAuthenticated.Returns(false);

        var result = _sut.IsAuthorized(ContentPolicies.EntryRead);

        result.Should().BeFalse("unauthenticated users are always denied");
    }

    [Fact]
    public void IsAuthorized_WhenUserHasRequiredRole_ReturnsTrue()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.Roles.Returns([Roles.Editor]);

        var result = _sut.IsAuthorized(ContentPolicies.EntryCreate);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsAuthorized_WhenUserLacksRequiredRole_ReturnsFalse()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.Roles.Returns([Roles.Viewer]);

        var result = _sut.IsAuthorized(ContentPolicies.EntryDelete);

        result.Should().BeFalse("Viewer role does not grant EntryDelete");
    }

    [Fact]
    public void IsAuthorized_WhenAllPoliciesGranted_ReturnsTrue()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.Roles.Returns([Roles.TenantAdmin]);

        var result = _sut.IsAuthorized(ContentPolicies.EntryCreate, ContentPolicies.EntryPublish);

        result.Should().BeTrue("TenantAdmin holds both EntryCreate and EntryPublish");
    }

    [Fact]
    public void IsAuthorized_WhenOnlyOneOfTwoPoliciesGranted_ReturnsFalse()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.Roles.Returns([Roles.Author]); // Author cannot publish

        var result = _sut.IsAuthorized(ContentPolicies.EntryCreate, ContentPolicies.EntryPublish);

        result.Should().BeFalse("Author lacks EntryPublish — all policies must be satisfied");
    }
}
