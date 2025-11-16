using Cart.Contract;

namespace Cart;

using FluentValidation;

public class CheckoutRequestValidator : AbstractValidator<CheckoutRequestDto>
{
    public CheckoutRequestValidator()
    {
        RuleFor(x => x.CustomerDetails).NotNull();
        RuleFor(x => x.CustomerDetails.FullName)
            .NotEmpty()
            .MinimumLength(2);
        
        RuleFor(x => x.CustomerDetails.PhoneNumber)
            .NotEmpty()
            .Matches(@"^\+?[1-9]\d{1,14}$")
            .WithMessage("Phone number must be in E.164 format (e.g. +1234567890).");

        RuleFor(x => x.ShippingAddress).NotNull();
        RuleFor(x => x.ShippingAddress.AddressLine)
            .NotEmpty()
            .MaximumLength(200);
        RuleFor(x => x.ShippingAddress.City).NotEmpty();
        RuleFor(x => x.ShippingAddress.PostalCode).NotEmpty();
        RuleFor(x => x.ShippingAddress.Country)
            .NotEmpty()
            .Length(2)
            .Matches(@"^[A-Z]{2}$")
            .WithMessage("Country must be a valid ISO 3166-1 alpha-2 code.");

        When(x => x.BillingAddress is not null, () =>
        {
            RuleFor(x => x.BillingAddress!.AddressLine)
                .NotEmpty()
                .MaximumLength(200);
            RuleFor(x => x.BillingAddress!.City).NotEmpty();
            RuleFor(x => x.BillingAddress!.PostalCode).NotEmpty();
            RuleFor(x => x.BillingAddress!.Country)
                .NotEmpty()
                .Length(2)
                .Matches(@"^[A-Z]{2}$")
                .WithMessage("Country must be a valid ISO 3166-1 alpha-2 code.");
        });

        RuleFor(x => x.ShippingMethod)
            .NotEmpty()
            .MaximumLength(50); 

        RuleFor(x => x.PaymentMethod)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.CustomerNotes)
            .MaximumLength(500)
            .When(x => x.CustomerNotes is not null);
    }
}