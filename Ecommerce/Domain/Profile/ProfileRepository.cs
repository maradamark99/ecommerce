using Ecommerce.Common.Data;
using Ecommerce.Domain.Profile.Contract;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Domain.Profile;

public class ProfileRepository(AppDbContext dbContext) : IProfileRepository
{
    public Task<CustomerDetails?> GetCustomerDetailsAsync(string customerId)
    {
        return dbContext.CustomerDetails
            .Include(c => c.DefaultBillingAddress)
            .Include(c => c.DefaultShippingAddress)
            .FirstOrDefaultAsync(c => c.AppUserId == customerId);
    }

    public Task SaveCustomerDetailsAsync(CustomerDetails? toUpdate, CustomerDetails updateWith)
    {
        if (toUpdate == null) 
        {
            dbContext.CustomerDetails.Add(updateWith);
        }
        else
        {
            toUpdate.FullName = updateWith.FullName;
            toUpdate.PhoneNumber = updateWith.PhoneNumber;
            toUpdate.Email = updateWith.Email;
            toUpdate.DefaultBillingAddress = updateWith.DefaultBillingAddress;
            toUpdate.DefaultShippingAddress = updateWith.DefaultShippingAddress;
        }
        return dbContext.SaveChangesAsync();
    }
}