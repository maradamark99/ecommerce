using Microsoft.AspNetCore.Identity;

namespace Auth;

public class AppUser : IdentityUser
{
    
    public List<RefreshToken>? RefreshTokens { get; set; }
    
}