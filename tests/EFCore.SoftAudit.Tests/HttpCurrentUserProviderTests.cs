using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace EFCore.SoftAudit.Tests;

public sealed class HttpCurrentUserProviderTests
{
    [Fact]
    public void GetCurrentUserId_ShouldReturnNull_WhenHttpContextIsNull()
    {
        var accessor = new HttpContextAccessor { HttpContext = null };
        var provider = new HttpCurrentUserProvider(accessor);
        provider.GetCurrentUserId().Should().BeNull();
    }

    [Fact]
    public void GetCurrentUserId_ShouldReturnNull_WhenNameIdentifierClaimIsMissing()
    {
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) };
        var accessor = new HttpContextAccessor { HttpContext = context };
        var provider = new HttpCurrentUserProvider(accessor);
        provider.GetCurrentUserId().Should().BeNull();
    }

    [Fact]
    public void GetCurrentUserId_ShouldReturnClaimValue_WhenNameIdentifierIsPresent()
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user-456")]);
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        var accessor = new HttpContextAccessor { HttpContext = context };
        var provider = new HttpCurrentUserProvider(accessor);
        provider.GetCurrentUserId().Should().Be("user-456");
    }

    [Fact]
    public void GetCurrentUserId_ShouldReturnClaimValue_WhenCustomClaimTypeIsConfigured()
    {
        var identity = new ClaimsIdentity([new Claim("sub", "user-789")]);
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        var accessor = new HttpContextAccessor { HttpContext = context };
        var provider = new HttpCurrentUserProvider(accessor, claimType: "sub");
        provider.GetCurrentUserId().Should().Be("user-789");
    }

    [Fact]
    public void GetCurrentUserId_ShouldReturnNull_WhenCustomClaimTypeIsConfiguredButAbsent()
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user-456")]);
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        var accessor = new HttpContextAccessor { HttpContext = context };
        var provider = new HttpCurrentUserProvider(accessor, claimType: "sub");
        provider.GetCurrentUserId().Should().BeNull();
    }
}
