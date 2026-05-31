using EFCore.SoftAudit.Interfaces;

namespace SampleApi;

public class Order:IAuditable,ISoftDeletable
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int Quantity { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}