using EFCore.SoftAudit;
using EFCore.SoftAudit.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EFCore.SoftAudit.Tests;

public class TestDbContext(
    DbContextOptions<TestDbContext> options,
    ICurrentUserProvider? currentUserProvider = null,
    ITimeProvider? timeProvider = null)
    : AuditableDbContext(options, currentUserProvider, timeProvider)
{
    public DbSet<TestOrder> Orders { get; set; }
}