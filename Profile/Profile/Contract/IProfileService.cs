using EcommerceLib.Auth;
using Profile.Data;

namespace Profile.Contract;

public interface IProfileService
{
    Task<CustomerDetails?> GetCustomerDetailsAsync(string customerId);
    
    Task SaveCustomerDetailsAsync(AppUser appUser, CustomerDetailsDto customerDetailsDto);
    
    Task HandleEmailConfirmedAsync(string customerId, string email);
}