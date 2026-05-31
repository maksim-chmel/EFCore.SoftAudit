using System.Data;
using System.Linq.Expressions;
using System.Security.Claims;
using EFCore.SoftAudit.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace EFCore.SoftAudit;

public abstract class AuditableDbContext(DbContextOptions options, IHttpContextAccessor? httpContextAccessor)
    : DbContext(options)
{
    private string? GetCurrentUser() => 
        httpContextAccessor?.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    private DateTime GetCurrentDateTime() => DateTime.UtcNow;

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var currentUser = GetCurrentUser();
        var now = GetCurrentDateTime();
        foreach (var entry in ChangeTracker.Entries())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (entry.Entity is IAuditable auditable)
                    {
                        auditable.CreatedAt = now;
                        auditable.CreatedBy = currentUser;
                    }
                    break;
                case EntityState.Deleted:
                    if (entry.Entity is ISoftDeletable deletable)
                    {
                        entry.State = EntityState.Modified;
                        deletable.IsDeleted = true;
                        deletable.DeletedAt = now;
                        deletable.DeletedBy = currentUser;
                    }
                    break;
                case EntityState.Modified:
                    if (entry.Entity is IAuditable au)
                    {
                        au.UpdatedAt = now;
                        au.UpdatedBy = currentUser;
                    }
                    break;
            }
            
        }
        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDeletable).IsAssignableFrom(entity.ClrType)) continue;
            var parameter = Expression.Parameter(entity.ClrType, "x");
            var property = Expression.Property(parameter,nameof(ISoftDeletable.IsDeleted));
            var condition = Expression.Equal(property, Expression.Constant(false));
            var lambda = Expression.Lambda(condition, parameter);
            modelBuilder.Entity(entity.ClrType).HasQueryFilter(lambda);
        }
    }
}