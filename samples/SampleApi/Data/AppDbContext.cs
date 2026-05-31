using EFCore.SoftAudit;
using Microsoft.EntityFrameworkCore;

namespace SampleApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor? httpContextAccessor)
    : AuditableDbContext(options, httpContextAccessor)
{
    public DbSet<Order> Orders { get; set; }

}