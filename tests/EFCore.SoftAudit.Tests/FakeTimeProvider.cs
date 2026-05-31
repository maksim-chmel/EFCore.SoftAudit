using EFCore.SoftAudit.Interfaces;

namespace EFCore.SoftAudit.Tests;

public class FakeTimeProvider(DateTime utcNow) : ITimeProvider
{
    public DateTime UtcNow { get; } = utcNow;
}
