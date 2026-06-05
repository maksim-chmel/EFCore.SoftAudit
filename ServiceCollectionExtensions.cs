using EFCore.SoftAudit.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;


namespace EFCore.SoftAudit;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSoftAudit<TContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> optionsAction,
        Action<SoftAuditOptions>? configureOptions = null)
        where TContext : AuditableDbContext
    {
        var options = new SoftAuditOptions();
        configureOptions?.Invoke(options);

        services.AddHttpContextAccessor();
        services.AddSingleton<ITimeProvider, SystemTimeProvider>();
        services.AddScoped<ICurrentUserProvider>(sp =>
            new HttpCurrentUserProvider(
                sp.GetRequiredService<IHttpContextAccessor>(),
                options.UserClaimType));
        services.AddDbContext<TContext>(optionsAction);
        return services;
    }
}