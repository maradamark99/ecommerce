using EcommerceLib.Auth;
using EcommerceLib.Exception;
using Profile.Contract;
using Profile.Data;

namespace Profile;

public class ProfileService(
    IProfileRepository profileRepository) : IProfileService
{
    public Task<CustomerDetails?> GetCustomerDetailsAsync(string customerId)
    {
        return profileRepository.GetCustomerDetailsAsync(customerId);
    }

    public async Task SaveCustomerDetailsAsync(AppUser appUser, CustomerDetailsDto customerDetailsDto)
    {
        if (customerDetailsDto == null)
        {
            throw new BadRequestException("Profile details cannot be null.");
        }
        var toUpdate = await profileRepository.GetCustomerDetailsAsync(appUser.Id);

        await profileRepository.SaveCustomerDetailsAsync(
            toUpdate, 
            MapToCustomerDetails(appUser, customerDetailsDto));
    }
    
    public async Task HandleEmailConfirmedAsync(string customerId, string email)
    {
        var customerDetails = await profileRepository.GetCustomerDetailsAsync(customerId);
        if (customerDetails != null)
        {
            return;
        }
        customerDetails = new CustomerDetails
        {
            CustomerId = customerId,
            Email = email,
            FullName = ExtractUsernameFromEmail(email)
        };
        await profileRepository.SaveCustomerDetailsAsync(null, customerDetails);
    }
    
    private static string ExtractUsernameFromEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        return atIndex > 0 ? email[..atIndex] : email;
    }   

    private static CustomerDetails MapToCustomerDetails(AppUser appUser, CustomerDetailsDto customerDetailsDto)
    {
        return new CustomerDetails
        {
            CustomerId = appUser.Id,
            FullName = customerDetailsDto.FullName,
            PhoneNumber = customerDetailsDto.PhoneNumber,
            DefaultShippingAddress = new Address() 
            {
                AddressLine = customerDetailsDto.DefaultShippingAddress.AddressLine,
                City = customerDetailsDto.DefaultShippingAddress.City,
                State = customerDetailsDto.DefaultShippingAddress.State,
                PostalCode = customerDetailsDto.DefaultShippingAddress.PostalCode,
                Country = customerDetailsDto.DefaultShippingAddress.Country
            },
            DefaultBillingAddress = customerDetailsDto.DefaultBillingAddress == null ? null : new Address() 
            {
                AddressLine = customerDetailsDto.DefaultBillingAddress.AddressLine,
                City = customerDetailsDto.DefaultBillingAddress.City,
                State = customerDetailsDto.DefaultBillingAddress.State,
                PostalCode = customerDetailsDto.DefaultBillingAddress.PostalCode,
                Country = customerDetailsDto.DefaultBillingAddress.Country
            }
        };
    }
}