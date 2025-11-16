using System.Net;
using EcommerceLib.Auth;
using EcommerceLib.Contract.Dto;
using EcommerceLib.Contract.Events;
using EcommerceLib.Exception;
using EcommerceLib.Messaging;
using OrderManagement.Common.Data;
using OrderManagement.Contract;
using OrderManagement.Inventory;
using OrderManagement.Payment;
using OrderManagement.Shipping.Contract;
using AddressDto = EcommerceLib.Contract.Dto.AddressDto;

namespace OrderManagement;

public class OrderManagementService(
    IInventoryClient inventoryClient,
    IShippingClient shippingClient,
    IPaymentClient paymentClient,
    IOrderRepository repository,
    IEventProducer<OrderEventDto> orderEventProducer) : IOrderManagementService
{
    
    public async Task CreateOrderAsync(CartCheckedOutEventDto cartEventDto)
    {
        var shippingRate = await shippingClient.GetShippingFeeAsync(cartEventDto.ShippingMethod);
        if (shippingRate.StatusCode != HttpStatusCode.OK || shippingRate.Fee is null)
        {
            await ProduceOrderCreationFailedEventAsync(cartEventDto, $"Failed to get shipping rate for payment method: {cartEventDto.ShippingMethod}");
            return;
        }
        var paymentRate = await paymentClient.GetPaymentFeeAsync(cartEventDto.PaymentMethod);
        if (paymentRate.StatusCode != HttpStatusCode.OK || paymentRate.Fee is null)
        {
            await ProduceOrderCreationFailedEventAsync(cartEventDto, $"Failed to get payment rate for payment method: {cartEventDto.PaymentMethod}");
            return;
        }
        var order = CreateOrder(cartEventDto, paymentRate.Fee, shippingRate.Fee);
        var response = await inventoryClient.TryReserveStockAsync(
            new StockReservationRequest(
                order.Id, 
                order.OrderItems.Select(
                    i => new StockReservationItem(i.ProductId, i.Quantity))
                    .ToList()
                )
            );
        if (!response.IsSuccessStatusCode)
        {
            await ProduceOrderCreationFailedEventAsync(cartEventDto,"Failed to reserve stock for items.");
            return;
        }
        await repository.CreateOrderAsync(order);
        await orderEventProducer.ProduceAsync(CreateOrderEvent(order, nameof(Events.OrderCreated)));
    }

    public async Task UpdateStatusAsync(string orderId, Status newStatus)
    {
        var order = await repository.GetByIdAsync(orderId)
            ?? throw new NotFoundException($"Order with ID {orderId} not found");
        
        order.StatusHistory.Add(new OrderStatus()
        {
            OrderId = orderId,
            Status = newStatus.ToString()
        });
        await repository.UpdateOrderAsync(order);
        if (newStatus == Status.Completed)
        {
            var completeEvent = CreateOrderEvent(order, nameof(Events.OrderCompleted));
            await orderEventProducer.ProduceAsync(completeEvent);   
        }
    }

    public async Task FulfillOrderAsync(string orderId)
    {
        var order = await repository.GetByIdAsync(orderId)
            ?? throw new NotFoundException($"Order with ID {orderId} not found");
        
        if (order.CurrentStatus != nameof(Status.Pending))
        {
            throw new BadRequestException($"Order with ID {orderId} is not in a valid state for fulfillment");
        }
        order.StatusHistory.Add(new OrderStatus() 
        {
            OrderId = order.Id,
            Status = nameof(Status.Created)
        });
        await repository.UpdateOrderAsync(order);
        var fulfilEvent = CreateOrderEvent(order, nameof(Events.FulfillOrder));
        await orderEventProducer.ProduceAsync(fulfilEvent);            
    }
    
    public async Task CancelOrderAsync(AppUser user, string orderId, string? reason)
    {
        var order = await repository.GetByIdAsync(orderId)
            ?? throw new NotFoundException($"Order with ID {orderId} not found");
        
        if (!user.Roles.Contains(nameof(Roles.Admin)) && order.CustomerDetails.CustomerId != user.Id)
        {
            throw new NotFoundException("Order not found");
        }
        if (order.CurrentStatus is nameof(Status.Cancelled) or nameof(Status.Completed))
        {
            throw new BadRequestException($"Order with ID {orderId} cannot be canceled as it is already {order.CurrentStatus}");
        }
        order.StatusHistory.Add(new OrderStatus() 
        {
            OrderId = order.Id,
            Status = nameof(Status.Cancelled)
        });   
        
        var cancelEvent = CreateOrderEvent(order, nameof(Events.OrderCancelled));
        await repository.UpdateOrderAsync(order); 
        await orderEventProducer.ProduceAsync(cancelEvent);
    }

    public Task<IEnumerable<Order>> GetOrderHistoryAsync(AppUser customer)
    {
        return repository.GetOrderHistoryAsync(customer.Id);  
    }

    public async Task<Order> GetByIdAsync(string customerId, string? checkoutId, string? orderId)
    {
        var order = await repository.GetByIdAsync(customerId, checkoutId, orderId) 
            ?? throw new NotFoundException($"Order with ID {orderId} not found");
        return order;
    }
    
    private async Task ProduceOrderCreationFailedEventAsync(CartCheckedOutEventDto cartEventDto, string reason)
    {
        var msg = new OrderEventDto()
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = nameof(Events.OrderCreationFailed),
            Timestamp = DateTime.UtcNow,
            Order = new OrderDto()
            {
                OrderId = cartEventDto.CheckoutId,
                Customer = new CustomerDto()
                {
                    CustomerId = cartEventDto.CustomerId
                }
            },
            FailureReason = reason
        };
        await orderEventProducer.ProduceAsync(msg);
    }

    private static Order CreateOrder(CartCheckedOutEventDto dto, PaymentFeeDto paymentFeeDto, Shipping.Contract.ShippingFeeDto shippingFeeDto)
    {
        var orderId = Guid.NewGuid().ToString();
        var shippingAddress = MapToAddress(dto.ShippingAddress);
        return new Order()
        {
            Id = orderId,
            CheckoutId = dto.CheckoutId,
            CustomerDetails = new CustomerDetails()
            {
                OrderId = orderId,
                CustomerId = dto.CustomerId,
                FullName = dto.CustomerDetails.FullName,
                Email = dto.CustomerDetails.Email,
                PhoneNumber = dto.CustomerDetails.PhoneNumber
            },
            EstimatedDeliveryDate = shippingFeeDto.EstimatedShippingDate is not null ? 
                DateTime.SpecifyKind(DateTime.Parse(shippingFeeDto.EstimatedShippingDate), DateTimeKind.Utc) 
                : null,
            OrderItems = dto.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList(),
            StatusHistory = [new OrderStatus() 
            {
                OrderId = orderId,
                Status = nameof(Status.Pending)
            }],
            PaymentMethod = dto.PaymentMethod,
            ShippingAddress = shippingAddress,
            BillingAddress = MapToAddress(dto.BillingAddress),
            ShippingMethod = dto.ShippingMethod,
            ShippingFee = shippingFeeDto.Fee,
            PaymentFee = paymentFeeDto.Fee,
            CustomerNotes = dto.CustomerNotes,
        };
    }
    
    private static OrderEventDto CreateOrderEvent(Order order, string eventType)
    {
        return new OrderEventDto()
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = eventType,
            Timestamp = DateTime.UtcNow,
            Order = new OrderDto()
            {
                OrderId = order.Id,
                Customer = new CustomerDto()
                {
                    CustomerId = order.CustomerDetails.CustomerId,
                    FullName = order.CustomerDetails.FullName,
                    Email = order.CustomerDetails.Email,
                    ShippingAddress = CreateAddressDto(order.ShippingAddress),
                    BillingAddress = CreateAddressDto(order.BillingAddress)
                },
                TotalAmount = order.Total,
                StatusHistory = order.StatusHistory
                    .Select(s => s.Status.ToString())
                    .ToList(),
                CreatedAt = order.CreatedAt,
                ShippingMethod = order.ShippingMethod,
                PaymentMethod = order.PaymentMethod,
                Items = order.OrderItems.Select(
                    i => new ItemDto()
                    {
                        ProductId = i.ProductId.ToString(),
                        Price = i.UnitPrice,
                        Quantity = i.Quantity
                    }).ToList()
            }
        };
    }
    
    private static Address MapToAddress(AddressDto addressDto)
    {
        return new Address
        {
            AddressLine = addressDto.AddressLine,
            City = addressDto.City,
            Country = addressDto.Country,
            PostalCode = addressDto.PostalCode,
            State = addressDto.State
        };
    }
    
    private static AddressDto? CreateAddressDto(Address address)
    {
        return new AddressDto()
        {
            AddressLine = address.AddressLine,
            City = address.City,
            Country = address.Country,
            PostalCode = address.PostalCode,
            State = address.State,
        };
    }

}