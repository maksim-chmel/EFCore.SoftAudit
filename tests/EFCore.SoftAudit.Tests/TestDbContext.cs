using Microsoft.EntityFrameworkCore;

namespace EFCore.SoftAudit.Tests;

public class TestDbContext(DbContextOptions<TestDbContext> options) : AuditableDbContext(options, null)
{
    public DbSet<TestOrder> Orders { get; set; }
}