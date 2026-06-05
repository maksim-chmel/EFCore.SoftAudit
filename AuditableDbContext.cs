using System.Linq.Expressions;
using EFCore.SoftAudit.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace EFCore.SoftAudit;

public abstract class AuditableDbContext(
    DbContextOptions options,
    ICurrentUserProvider? currentUserProvider = null,
    ITimeProvider? timeProvider = null)
    : DbContext(options)
{
    [Obsolete("Use the constructor with ICurrentUserProvider and ITimeProvider. Register them via AddSoftAudit<TContext>() in your DI setup.")]
    protected AuditableDbContext(DbContextOptions options, IHttpContextAccessor? httpContextAccessor)
        : this(options, httpContextAccessor != null ? new HttpCurrentUserProvider(httpContextAccessor) : null) {}

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
                case EntityState.Added when entry.Entity is  IAuditable auditable:
                    
                        auditable.CreatedAt = now;
                        auditable.CreatedBy = currentUser;
                    
                    break;
                case EntityState.Deleted when entry.Entity is ISoftDeletable deletable:
                    
                        entry.State = EntityState.Modified;
                        deletable.IsDeleted = true;
                        deletable.DeletedAt = now;
                        deletable.DeletedBy = currentUser;
                    
                    break;
                case EntityState.Modified when entry.Entity is IAuditable au 
                                               && entry.Entity is not ISoftDeletable { IsDeleted: true }:
                    
                        au.UpdatedAt = now;
                        au.UpdatedBy = currentUser;
                        
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

    public void Restore<TEntity>(TEntity entity)
        where TEntity : class, ISoftDeletable
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.DeletedBy = null;
        Update(entity);
    }

    public void RestoreRange<TEntity>(IEnumerable<TEntity> entities)
        where TEntity : class, ISoftDeletable
    {
        if (entities is null) throw new ArgumentNullException(nameof(entities));
        foreach (var entity in entities)
            Restore(entity);
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        foreach (var entity in modelBuilder.Model.GetEntityTypes()
                     .Where(e => typeof(ISoftDeletable).IsAssignableFrom(e.ClrType)))
        {
            modelBuilder.Entity(entity.ClrType)
                .HasQueryFilter(BuildIsNotDeletedFilter(entity.ClrType));
        }
    }

    private static LambdaExpression BuildIsNotDeletedFilter(Type type)
    {
        var param = Expression.Parameter(type, "x");
        var prop = Expression.Property(param, nameof(ISoftDeletable.IsDeleted));
        var body = Expression.Equal(prop, Expression.Constant(false));
        return Expression.Lambda(body, param);
    }
}