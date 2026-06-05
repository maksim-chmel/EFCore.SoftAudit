using EFCore.SoftAudit.Interfaces;

namespace EFCore.SoftAudit.Tests;

public sealed class FakeUserProvider(string userId) : ICurrentUserProvider
{
    public string? GetCurrentUserId() => userId;
}
