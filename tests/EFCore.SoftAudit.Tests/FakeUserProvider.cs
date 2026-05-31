using EFCore.SoftAudit.Interfaces;

namespace EFCore.SoftAudit.Tests;

public class FakeUserProvider(string userId) : ICurrentUserProvider
{
    public string? GetCurrentUserId() => userId;
}
