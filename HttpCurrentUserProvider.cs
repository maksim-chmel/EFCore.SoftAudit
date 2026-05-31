using System.Security.Claims;
using EFCore.SoftAudit.Interfaces;
using Microsoft.AspNetCore.Http;

namespace EFCore.SoftAudit;

public class HttpCurrentUserProvider(IHttpContextAccessor accessor):ICurrentUserProvider
{
    public string? GetCurrentUserId() => 
        accessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
}