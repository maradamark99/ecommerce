using Ecommerce.Common.Data;
using Ecommerce.Common.Exception;
using Ecommerce.Domain.Auth;
using Ecommerce.Domain.Cart.Contract;
using Ecommerce.Domain.Inventory.Contract;
using Ecommerce.Domain.Notification;
using Ecommerce.Domain.Notification.Contract;
using Ecommerce.Domain.OrderManagement.Contract;
using Ecommerce.Domain.Payment;
using Ecommerce.Domain.Payment.Contract;
using Ecommerce.Domain.Shipping;
using Ecommerce.Domain.Shipping.Contract;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.Domain.OrderManagement;

public class OrderManagementService(
    UserManager<AppUser> userManager,
    ICartService cartService,
    IInventoryService inventoryService,
    INotificationHandlerFactory notificationHandlerFactory,
    IPaymentService paymentService,
    IShippingService shippingService,
    IUnitOfWork unitOfWork) : IOrderManagementService
{
    
    public async Task<OrderSummary> CreateAsync(string customerId, CreateOrderRequest createRequest)
    {
        var customer = await userManager.FindByIdAsync(customerId);
        if (customer is null)
        {
            throw new NotFoundException($"Customer with ID {customerId} not found");
        }
        var items = (await cartService.GetByIdAsync(customerId)).Products.ToList();
        if (items.IsNullOrEmpty())
        {
            throw new BadRequestException("Items must not be empty");
        }

        var shippingRate = await shippingService.GetShippingRateAsync(createRequest.ShippingMethod);
        var paymentRate = await paymentService.GetPaymentRateAsync(createRequest.PaymentMethod);
        var order = CreateOrder(createRequest, items, paymentRate, shippingRate, customer);
        await inventoryService.ReserveStockAsync(order.OrderItems);
   
        var orderSummary = new OrderSummary()
        {
            OrderId = order.Id,
            Items = order.OrderItems,
            CustomerDetails = order.CustomerDetails,
            Status = order.CurrentStatus,
            PaymentRate = paymentRate,
            ShippingRate = shippingRate,
            ShippingAddress = createRequest.ShippingAddress,
            BillingAddress = createRequest.BillingAddress,
        };
        await unitOfWork.Orders.CreateOrderAsync(order);
        await cartService.ClearCartAsync(customerId);
        return orderSummary;
    }
    
    public async Task FulfillOrderAsync(string orderId)
    {
        try 
        {
            await unitOfWork.BeginTransactionAsync();
            var order = await unitOfWork.Orders.GetByIdAsync(orderId);
            if (order is null) 
            {
                throw new NotFoundException($"Order with ID {orderId} not found");
            }
            if (order.CurrentStatus != Status.Pending)
            {
                throw new BadRequestException($"Order with ID {orderId} is not in a valid state for fulfillment");
            }
            order.StatusHistory.Add(new OrderStatus() 
            {
                OrderId = order.Id,
                Status = Status.Created
            });
            await shippingService.CreateShipmentAsync(new ShipmentRequest() 
            {
                Order = order
            });
            await unitOfWork.Orders.FulfillOrderAsync(order);
            var notificationService = notificationHandlerFactory.Create<EmailNotificationRequest>();
            var notificationRequest = CreateNotificationEmail(
                order.CustomerDetails.Email,
                "Your order has been fulfilled.",
                $"Your order with ID {order.Id} has been fulfilled successfully. Thank you for shopping with us!"
            );            
            await notificationService.NotifyAsync(notificationRequest);            
        }
        catch (Exception)
        {
            await unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
    
    public async Task CancelOrderAsync(string userId, string orderId)
    {
        try
        {
            await unitOfWork.BeginTransactionAsync();
            var order = await unitOfWork.Orders.GetByIdAsync(orderId);
            if (order is null) 
            {
                throw new NotFoundException($"Order with ID {orderId} not found");
            }
            var user = await userManager.FindByIdAsync(userId);
            var roles = await userManager.GetRolesAsync(user!);
            if (!roles.Contains(nameof(Roles.Admin)) && order.AppUser.Id != userId) 
            {
                throw new UnauthorizedException("You do not have permission to delete this order.");
            }
            if (order.CurrentStatus is Status.Canceled or Status.Delivered)
            {
                throw new BadRequestException($"Order with ID {orderId} cannot be canceled as it is already {order.CurrentStatus}");
            }
            if (order.StatusHistory.Any(s => s.Status == Status.Created))
            {
                await shippingService.CancelShipmentAsync(orderId); 
            }
            if (order.StatusHistory.Any(s => s.Status == Status.Paid))
            {
                await paymentService.CreateRefundAsync(order);
            }
            await inventoryService.ReleaseStockAsync(order.OrderItems);
            order.StatusHistory.Add(new OrderStatus() 
            {
                OrderId = order.Id,
                Status = Status.Canceled
            });   
            await unitOfWork.Orders.UpdateOrderAsync(order);            
            await unitOfWork.CommitTransactionAsync();
        }
        catch (Exception)
        {
            await unitOfWork.RollbackTransactionAsync();
            throw;
        }   
    }

    public async Task MarkOrderAsExpiredAsync(string orderId)
    {
        try
        {
            await unitOfWork.BeginTransactionAsync();

            var order = await unitOfWork.Orders.GetByIdAsync(orderId);
            if (order is null)
            {
                throw new NotFoundException($"Order with ID {orderId} not found");
            }

            if (order.CurrentStatus != Status.Pending)
            {
                throw new BadRequestException($"Order with ID {orderId} cannot be marked as expired because it is in status {order.CurrentStatus}");
            }

            await inventoryService.ReleaseStockAsync(order.OrderItems);

            order.StatusHistory.Add(new OrderStatus()
            {
                OrderId = order.Id,
                Status = Status.Expired
            });

            await unitOfWork.Orders.UpdateOrderAsync(order);

            var notificationService = notificationHandlerFactory.Create<EmailNotificationRequest>();
            var notificationRequest = CreateNotificationEmail(
                order.CustomerDetails.Email,
                "Your order has expired",
                $"Your order with ID {order.Id} has expired because payment was not completed in time."
            );
            await notificationService.NotifyAsync(notificationRequest);

            await unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public Task<IEnumerable<Order>> GetOrderHistoryAsync(string customerId)
    {
        return unitOfWork.Orders.GetOrderHistoryAsync(customerId);  
    }

    public async Task<Order?> GetByIdAsync(string customerId, string orderId)
    {
        var order = await unitOfWork.Orders.GetByIdAsync(orderId);
        if (order is null)
        {
            return null;
        }
        if (order.AppUser.Id != customerId)
        {
            throw new UnauthorizedException("You do not have permission to view this order");
        }
        return order;
    }
    
    public Task<Order?> GetByIdAsync(string orderId)
    {
        return unitOfWork.Orders.GetByIdAsync(orderId);
    }
    
    public Task UpdateOrderAsync(Order order)
    {
        if (order is null)
        {
            throw new BadRequestException("Order must not be null");
        }
        return unitOfWork.Orders.UpdateOrderAsync(order);
    }

    private static Order CreateOrder(CreateOrderRequest createRequest, List<CartItemResponse> items,
        PaymentRate paymentRate, ShippingRate shippingRate, AppUser customer)
    {
        var orderId = Guid.NewGuid().ToString(); 
        return new Order()
        {
            Id = orderId,
            AppUser = customer,
            CustomerDetails = new CustomerDetails()
            {
                AppUserId = customer.Id,
                FullName = createRequest.CustomerDetailsRequest.FullName,
                Email = createRequest.CustomerDetailsRequest.Email,
                PhoneNumber = createRequest.CustomerDetailsRequest.PhoneNumber
            },
            EstimatedDeliveryDate = shippingRate.EstimatedShippingDate,
            OrderItems = items.Select(i => new OrderItem
            {
                ProductId = i.ProductId.ToString(),
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList(),
            StatusHistory = [new OrderStatus() 
            {
                OrderId = orderId,
                Status = Status.Pending
            }],
            PaymentMethod = paymentRate.PaymentMethod.ToString(),
            ShippingAddress = createRequest.ShippingAddress,
            BillingAddress = createRequest.BillingAddress ?? createRequest.ShippingAddress,
            ShippingMethod = shippingRate.Method.ToString(),
            ShippingFee = shippingRate.Fee,
            PaymentFee = paymentRate.Fee,
            CustomerNotes = createRequest.CustomerNotes,
        };
    }
    
    private static EmailNotificationRequest CreateNotificationEmail(string to, string subject, string message)
    {
        return new EmailNotificationRequest()
        {
            To = to,
            Subject = subject,
            Message = message
        };
    }
}