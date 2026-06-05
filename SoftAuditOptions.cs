using System.Security.Claims;

namespace EFCore.SoftAudit;

public class SoftAuditOptions
{
    public string UserClaimType { get; set; } = ClaimTypes.NameIdentifier;
}
