namespace EcommerceLib.Auth;

public class AuthConfig
{
    public string JwtIssuer { get; set; }
    
    public string JwtAudience { get; set; }
    
    public string JwtSecretKey { get; set; }
    
    public int RefreshTokenLength { get; set; }
}