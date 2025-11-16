namespace Ecommerce.Domain.Notification.Contract;

public abstract class NotificationRequest
{
    public required string Message { get; set; }
}