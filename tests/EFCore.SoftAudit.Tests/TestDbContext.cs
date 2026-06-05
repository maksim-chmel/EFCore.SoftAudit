using EFCore.SoftAudit;
using EFCore.SoftAudit.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EFCore.SoftAudit.Tests;

public sealed class TestDbContext(
    DbContextOptions<TestDbContext> options,
    ICurrentUserProvider? currentUserProvider = null,
    ITimeProvider? timeProvider = null)
    : AuditableDbContext(options, currentUserProvider, timeProvider)
{
    public DbSet<TestOrder> Orders { get; set; }
    public DbSet<TestSoftOnlyOrder> SoftOnlyOrders { get; set; }
    public DbSet<TestAuditOnlyOrder> AuditOnlyOrders { get; set; }
}