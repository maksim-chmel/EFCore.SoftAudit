using System.Linq.Expressions;
using EFCore.SoftAudit.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EFCore.SoftAudit;

public abstract class AuditableDbContext(
    DbContextOptions options,
    ICurrentUserProvider? currentUserProvider = null,
    ITimeProvider? timeProvider = null)
    : DbContext(options)
{
    private string? GetCurrentUser() => currentUserProvider?.GetCurrentUserId();
    private DateTime GetCurrentDateTime() => timeProvider?.UtcNow ?? DateTime.UtcNow;

    private void ApplyAuditRules()
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
                        if (entry.Entity is ISoftDeletable { IsDeleted: true })
                        {
                            break;
                        }

                        au.UpdatedAt = now;
                        au.UpdatedBy = currentUser;
                    }

                    break;
            }
        }

    }

    public override int SaveChanges()
    {
        ApplyAuditRules();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditRules();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditRules();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAuditRules();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
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