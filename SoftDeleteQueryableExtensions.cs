using EFCore.SoftAudit.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EFCore.SoftAudit;

public static class SoftDeleteQueryableExtensions
{
    // Removes the global soft-delete query filter so deleted entities are included.
    // Note: IgnoreQueryFilters removes ALL global query filters on the entity type.
    public static IQueryable<T> WithDeleted<T>(this IQueryable<T> source)
        where T : class, ISoftDeletable
        => source.IgnoreQueryFilters();

    public static IQueryable<T> OnlyDeleted<T>(this IQueryable<T> source)
        where T : class, ISoftDeletable
        => source.IgnoreQueryFilters().Where(x => x.IsDeleted);
}
