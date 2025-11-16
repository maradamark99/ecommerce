using Ecommerce.Common.Data;
using Ecommerce.Domain.Profile.Contract;

namespace Ecommerce.Domain.Profile;

public class ProfileService(IProfileRepository profileRepository) : IProfileService
{
    public Task<CustomerDetails?> GetCustomerDetailsAsync(string customerId)
    {
        return profileRepository.GetCustomerDetailsAsync(customerId);
    }

    public async Task SaveCustomerDetailsAsync(string customerId, ProfileCustomerDetailsDto profileDetails)
    {
        if (profileDetails == null)
        {
            throw new ArgumentNullException(nameof(profileDetails), "Profile details cannot be null.");
        }
        var toUpdate = await profileRepository.GetCustomerDetailsAsync(customerId.ToString());

        await profileRepository.SaveCustomerDetailsAsync(
            toUpdate, 
            MapToCustomerDetails(customerId, profileDetails));
    }
    
    private static CustomerDetails MapToCustomerDetails(string appUserId, ProfileCustomerDetailsDto profileDetails)
    {
        return new CustomerDetails
        {
            AppUserId = appUserId,
            FullName = profileDetails.FullName,
            Email = profileDetails.Email,
            PhoneNumber = profileDetails.PhoneNumber,
            DefaultShippingAddress = new Address() 
            {
                AddressLine = profileDetails.DefaultShippingAddress.AddressLine,
                City = profileDetails.DefaultShippingAddress.City,
                State = profileDetails.DefaultShippingAddress.State,
                PostalCode = profileDetails.DefaultShippingAddress.PostalCode,
                Country = profileDetails.DefaultShippingAddress.Country
            },
            DefaultBillingAddress = profileDetails.DefaultBillingAddress == null ? null :new Address() 
            {
                AddressLine = profileDetails.DefaultBillingAddress.AddressLine,
                City = profileDetails.DefaultBillingAddress.City,
                State = profileDetails.DefaultBillingAddress.State,
                PostalCode = profileDetails.DefaultBillingAddress.PostalCode,
                Country = profileDetails.DefaultBillingAddress.Country
            }
        };
    }
}