using Cart.Contract;
using EcommerceLib.Contract.Dto;

namespace Cart.Tests;

public static class TestDataHelper
{

    public static CheckoutRequestDto CreateCheckoutRequestDto()
    {
        return new CheckoutRequestDto(
            CustomerDetails: new CustomerDetailsDto(
                FullName: "Jane Doe",
                Email: "jane.doe@example.com",
                PhoneNumber: "+15551234567"
            ),
            ShippingAddress: new AddressDto
            {
                AddressLine = "123 Main Street",
                City = "Springfield",
                State = "IL",
                PostalCode = "62704",
                Country = "US"
            },
            BillingAddress: new AddressDto
            {
                AddressLine = "456 Oak Avenue",
                City = "Springfield",
                State = "IL",
                PostalCode = "62705",
                Country = "US"
            },
            ShippingMethod: "Standard",
            PaymentMethod: "CreditCard",
            CustomerNotes: "Leave package at the front door"
        );
    }
    
}