using EFCore.SoftAudit.Interfaces;

namespace EFCore.SoftAudit.Tests;

public sealed class TestSoftOnlyOrder : ISoftDeletable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
