namespace Ecommerce.Domain.Notification.Contract;

public interface INotificationHandler<in T> where T : NotificationRequest
{
    public Task NotifyAsync(T notificationRequest);
    
}