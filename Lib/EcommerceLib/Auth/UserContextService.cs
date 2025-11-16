using EcommerceLib.Contract;
using Microsoft.AspNetCore.Http;

namespace EcommerceLib.Auth;

public class UserContextService(IHttpContextAccessor httpContextAccessor) : IUserContextService
{
    public AppUser? GetUser()
    {
        var headers = httpContextAccessor.HttpContext?.Request.Headers;
        
        if (headers == null) return null;
        
        var userId = headers[AuthHeaders.UserId].FirstOrDefault();
        var email = headers[AuthHeaders.UserEmail].FirstOrDefault();
        var rolesString = headers[AuthHeaders.UserRoles].FirstOrDefault(); 
        
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(rolesString)) return null;
        return new AppUser { Id = userId, Email = email, Roles = rolesString.Split(',').ToList() };
    }
}