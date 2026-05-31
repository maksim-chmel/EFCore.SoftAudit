namespace EFCore.SoftAudit.Interfaces;

public interface ITimeProvider
{
    DateTime UtcNow { get; }
}
