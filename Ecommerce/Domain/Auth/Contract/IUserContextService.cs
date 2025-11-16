using System.Security.Claims;

namespace Ecommerce.Domain.Auth.Contract;

public interface IUserContextService
{
    
    public bool IsAuthenticated();
    
    public ClaimsPrincipal? GetPrincipal();
    
    public string? GetUserId();

    public string? GetIpAddress();

}