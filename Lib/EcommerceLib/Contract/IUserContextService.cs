using EcommerceLib.Auth;

namespace EcommerceLib.Contract;

public interface IUserContextService
{
    AppUser? GetUser();
}