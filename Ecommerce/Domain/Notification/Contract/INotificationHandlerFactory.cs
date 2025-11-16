namespace Ecommerce.Domain.Notification.Contract;

public interface INotificationHandlerFactory
{
    INotificationHandler<T> Create<T>() where T : NotificationRequest;
}