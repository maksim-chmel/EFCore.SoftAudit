using EFCore.SoftAudit;
using EFCore.SoftAudit.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace SampleApi.Data;

public class AppDbContext(
    DbContextOptions<AppDbContext> options,
    ICurrentUserProvider? currentUserProvider,
    ITimeProvider? timeProvider)
    : AuditableDbContext(options, currentUserProvider, timeProvider)
{
    public DbSet<Order> Orders { get; set; }

}