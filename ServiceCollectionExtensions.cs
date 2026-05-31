using EFCore.SoftAudit.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;


namespace EFCore.SoftAudit;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSoftAudit<TContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> optionsAction)
        where TContext : AuditableDbContext
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<ITimeProvider, SystemTimeProvider>();
        services.AddScoped<ICurrentUserProvider, HttpCurrentUserProvider>();
        services.AddDbContext<TContext>(optionsAction);
        return services;
    }
}