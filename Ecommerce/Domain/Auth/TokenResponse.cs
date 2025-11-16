namespace Ecommerce.Domain.Auth;

public record TokenResponse(string AccessToken, string RefreshToken);