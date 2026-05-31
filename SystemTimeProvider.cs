using EFCore.SoftAudit.Interfaces;

namespace EFCore.SoftAudit;

public class SystemTimeProvider : ITimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
