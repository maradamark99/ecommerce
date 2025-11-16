using Microsoft.AspNetCore.Identity;

namespace Ecommerce.Domain.Auth;

public class AppRole : IdentityRole
{
    public AppRole()
    {
        
    }

    public AppRole(string roleName) : base(roleName)
    {
    }
}