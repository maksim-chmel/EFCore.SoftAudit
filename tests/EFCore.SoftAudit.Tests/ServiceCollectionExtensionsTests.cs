using EFCore.SoftAudit.Interfaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EFCore.SoftAudit.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSoftAudit_ShouldRegisterCurrentUserProvider_AsHttpCurrentUserProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSoftAudit<TestDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var userProvider = scope.ServiceProvider.GetRequiredService<ICurrentUserProvider>();
        userProvider.Should().BeOfType<HttpCurrentUserProvider>();
    }

    [Fact]
    public void AddSoftAudit_ShouldRegisterTimeProvider_AsSystemTimeProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSoftAudit<TestDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        var provider = services.BuildServiceProvider();
        var timeProvider = provider.GetRequiredService<ITimeProvider>();
        timeProvider.Should().BeOfType<SystemTimeProvider>();
    }

    [Fact]
    public void AddSoftAudit_ShouldRegisterDbContext()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSoftAudit<TestDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        context.Should().NotBeNull();
    }

    [Fact]
    public void AddSoftAudit_ShouldUseNameIdentifier_WhenNoOptionsConfigured()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSoftAudit<TestDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var userProvider = scope.ServiceProvider.GetRequiredService<ICurrentUserProvider>();
        var identity = new System.Security.Claims.ClaimsIdentity(
            [new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "user-default")]);
        var accessor = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        accessor.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = new System.Security.Claims.ClaimsPrincipal(identity)
        };

        userProvider.GetCurrentUserId().Should().Be("user-default");
    }

    [Fact]
    public void AddSoftAudit_ShouldUseCustomClaimType_WhenOptionsAreConfigured()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSoftAudit<TestDbContext>(
            o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()),
            o => o.UserClaimType = "sub");

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var userProvider = scope.ServiceProvider.GetRequiredService<ICurrentUserProvider>();
        var httpContext = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(
                [new System.Security.Claims.Claim("sub", "user-from-sub")]));

        var accessor = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        accessor.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = httpContext };

        userProvider.GetCurrentUserId().Should().Be("user-from-sub");
    }
}
