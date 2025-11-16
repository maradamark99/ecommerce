using Ecommerce.Common.Data;

namespace Ecommerce.Domain.Profile.Contract;

public interface IProfileService
{
    Task<CustomerDetails?> GetCustomerDetailsAsync(string customerId);
    
    Task SaveCustomerDetailsAsync(string customerId, ProfileCustomerDetailsDto profileDetails);
}