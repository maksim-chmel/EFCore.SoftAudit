using Microsoft.EntityFrameworkCore;

namespace EFCore.SoftAudit.Tests;

[Obsolete("Obsolete")]
public sealed class ObsoleteTestDbContext(
    DbContextOptions<ObsoleteTestDbContext> options,
    Microsoft.AspNetCore.Http.IHttpContextAccessor? httpContextAccessor)
    : AuditableDbContext(options, httpContextAccessor);