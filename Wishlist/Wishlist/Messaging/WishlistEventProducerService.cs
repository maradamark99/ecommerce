using EcommerceLib.Contract.Events;
using EcommerceLib.Messaging;
using Microsoft.Extensions.Options;

namespace Wishlist.Messaging;

public class WishlistEventProducerService(
    IOptions<ProducerOptions> options,
    ILogger<EventProducerBase<WishlistEventDto>> logger)
    : EventProducerBase<WishlistEventDto>(options, logger);