using Microsoft.EntityFrameworkCore;
using Profile.Contract;
using CustomerDetails = Profile.Data.CustomerDetails;

namespace Profile;

public class ProfileRepository(AppDbContext dbContext) : IProfileRepository
{
    public Task<CustomerDetails?> GetCustomerDetailsAsync(string customerId)
    {
        return dbContext.CustomerDetails
            .Include(c => c.DefaultBillingAddress)
            .Include(c => c.DefaultShippingAddress)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId);
    }

    public async Task SaveCustomerDetailsAsync(CustomerDetails? toUpdate, CustomerDetails updateWith)
    {
        if (toUpdate == null) 
        {
            await dbContext.CustomerDetails.AddAsync(updateWith);
        }
        else
        {
            toUpdate.FullName = updateWith.FullName;
            toUpdate.Email = updateWith.Email;
            toUpdate.PhoneNumber = updateWith.PhoneNumber;
            toUpdate.DefaultBillingAddress = updateWith.DefaultBillingAddress;
            toUpdate.DefaultShippingAddress = updateWith.DefaultShippingAddress;
        }
        await dbContext.SaveChangesAsync();
    }
    
}