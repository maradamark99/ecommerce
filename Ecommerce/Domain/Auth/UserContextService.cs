using System.Security.Claims;
using Ecommerce.Domain.Auth.Contract;

namespace Ecommerce.Domain.Auth;

public class UserContextService(IHttpContextAccessor httpContextAccessor)  : IUserContextService
{
    public bool IsAuthenticated() 
    {
        return httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
    }
    
    public ClaimsPrincipal? GetPrincipal() 
    {
        return httpContextAccessor.HttpContext?.User ?? null;
    }
    
    public string? GetUserId()
    {
        return httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    public string? GetIpAddress()
    {
        return httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    }
}