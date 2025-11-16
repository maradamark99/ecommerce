namespace Ecommerce.Domain.OrderManagement;

public enum Status
{
    Created,
    Pending,
    EnRoute,
    Canceled,
    Delivered,
    Paid,
    Expired,
    Failed
}