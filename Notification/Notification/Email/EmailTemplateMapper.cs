using System.Globalization;
using EcommerceLib.Contract.Events;
using Notification.Clients;

namespace Notification.Email;

public class EmailTemplateMapper(IEmailTemplateProvider emailTemplateProvider) : IEmailTemplateMapper
{
    public EmailPayload MapOrderEventToEmailPayload(OrderEventDto dto, CustomerDetailsDto customerDetailsDto)
    {
        var template = emailTemplateProvider.GetTemplate(dto.EventType);
        if (template == null) throw new NullReferenceException("Order event template is null");
        template.Body = template.Body
            .Replace("{{OrderId}}", dto.Order.OrderId)
            .Replace("{{CustomerName}}", dto.Order.Customer.FullName);

        return new EmailPayload()
        {
            To = customerDetailsDto.Email,
            Message = template.Body,
            Subject = template.Subject
        };
    }
    
    public EmailPayload MapShippingEventToEmailPayload(ShippingEventDto dto, CustomerDetailsDto customerDetailsDto)
    {
        var template = emailTemplateProvider.GetTemplate(dto.EventType);
        if (template == null) throw new NullReferenceException("Shipping event template is null");
        template.Body = template.Body
            .Replace("{{OrderId}}", dto.OrderId)
            .Replace("{{CustomerName}}", dto.Customer.FullName);

        return new EmailPayload()
        {
            To = customerDetailsDto.Email,
            Message = template.Body,
            Subject = template.Subject
        };
    }

    public EmailPayload MapWishlistEventToEmailPayload(WishlistEventDto dto, CustomerDetailsDto customerDto)
    {
        var template = emailTemplateProvider.GetTemplate(dto.EventType);
        if (template == null) throw new NullReferenceException("Wishlist event template is null");
        switch (dto.EventType) 
        {
            case nameof(Events.WishlistItemDiscounted):
                var discountedProduct = dto.Item;
                template.Body = template.Body
                    .Replace("{{CustomerName}}", customerDto.FullName)
                    .Replace("{{ProductName}}", discountedProduct.Name)
                    .Replace("{{ProductPrice}}", discountedProduct.Price.ToString("C", new CultureInfo("de-DE")))
                    .Replace("{{DiscountedPrice}}", discountedProduct.DiscountedPrice?.ToString("de-DE") ?? string.Empty);
                break;
            case nameof(Events.WishlistItemInStock):
                var productInStock = dto.Item;
                template.Body = template.Body
                    .Replace("{{CustomerName}}", customerDto.FullName)
                    .Replace("{{ProductName}}", productInStock.Name)
                    .Replace("{{ProductPrice}}", productInStock.Price.ToString("de-DE"));
                break;
            default:
                throw new NotSupportedException($"Event type {dto.EventType} not supported");
        }

        return new EmailPayload()
        {
            To = customerDto.Email,
            Message = template.Body,
            Subject = template.Subject
        };
    }
}