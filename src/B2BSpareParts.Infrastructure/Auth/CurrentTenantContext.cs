using System.Security.Claims;
using B2BSpareParts.Application.Contracts;
using B2BSpareParts.Common;
using Microsoft.AspNetCore.Http;

namespace B2BSpareParts.Infrastructure.Auth;

public class CurrentTenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentTenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid TenantId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var claimValue = user?.FindFirstValue("tenantId");
            if (Guid.TryParse(claimValue, out var tenantId))
            {
                return tenantId;
            }

            var headerValue = _httpContextAccessor.HttpContext?.Request.Headers[ApiConstants.TenantHeader].FirstOrDefault();
            if (Guid.TryParse(headerValue, out tenantId))
            {
                return tenantId;
            }

            return Guid.Empty;
        }
    }

    public Guid? UserId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Role => _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role);
}
