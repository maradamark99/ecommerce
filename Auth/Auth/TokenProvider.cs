using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Auth.Contract;
using EcommerceLib.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Auth;

public class TokenProvider(IOptions<AuthConfig> authConfig) : ITokenProvider
{
    
    public string GenerateToken(AppUser user, IList<string> roles)
    {
        var tokenHandler = new JsonWebTokenHandler();
        var secret = Encoding.Default.GetBytes(authConfig.Value.JwtSecretKey);

        var claims = new List<Claim>
        {
            new (JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new (JwtRegisteredClaimNames.Sub, user.Id),
            new (JwtRegisteredClaimNames.Email, user.Email!)
        };
        claims.AddRange(roles.Select(role => new Claim("role", role)));
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(25),
            Issuer = authConfig.Value.JwtIssuer,
            Audience = authConfig.Value.JwtAudience,
            SigningCredentials =
                new SigningCredentials(new SymmetricSecurityKey(secret), SecurityAlgorithms.HmacSha256Signature)
        };
        
        return tokenHandler.CreateToken(tokenDescriptor);
    }

    public string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(authConfig.Value.RefreshTokenLength));
    }
    
}