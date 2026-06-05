using EFCore.SoftAudit.Interfaces;

namespace EFCore.SoftAudit.Tests;

public sealed class FakeTimeProvider(DateTime utcNow) : ITimeProvider
{
    public DateTime UtcNow { get; } = utcNow;
}
