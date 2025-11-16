namespace Ecommerce.Domain.Auth.Contract;

public interface ITokenProvider
{
    string GenerateToken(AppUser user);

    string GenerateRefreshToken();
}