using Microsoft.AspNetCore.Identity;

namespace Auth;

public class AppRole : IdentityRole
{
    public AppRole()
    {
        
    }

    public AppRole(string roleName) : base(roleName)
    {
    }
}