using EcommerceLib.Contract.Events;
using Notification.Clients;

namespace Notification.Email;

public interface IEmailTemplateMapper
{
    EmailPayload MapOrderEventToEmailPayload(OrderEventDto dto, CustomerDetailsDto customerDto);

    EmailPayload MapShippingEventToEmailPayload(ShippingEventDto dto, CustomerDetailsDto customerDto);
    
    EmailPayload MapWishlistEventToEmailPayload(WishlistEventDto dto, CustomerDetailsDto customerDto);
}