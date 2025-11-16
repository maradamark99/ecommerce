using Profile.Data;

namespace Profile.Contract;

public interface IProfileRepository
{
    Task<CustomerDetails?> GetCustomerDetailsAsync(string customerId);
    
    Task SaveCustomerDetailsAsync(CustomerDetails? toUpdate, CustomerDetails updateWith);
}