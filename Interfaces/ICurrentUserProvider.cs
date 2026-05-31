namespace EFCore.SoftAudit.Interfaces;

public interface ICurrentUserProvider
{
    string? GetCurrentUserId();
}