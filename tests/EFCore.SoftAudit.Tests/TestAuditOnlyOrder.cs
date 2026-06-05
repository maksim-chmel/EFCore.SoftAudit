using EFCore.SoftAudit.Interfaces;

namespace EFCore.SoftAudit.Tests;

public sealed class TestAuditOnlyOrder : IAuditable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
