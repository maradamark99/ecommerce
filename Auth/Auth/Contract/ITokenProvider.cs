namespace Auth.Contract;

public interface ITokenProvider
{
    string GenerateToken(AppUser user, IList<string> roles);

    string GenerateRefreshToken();
}