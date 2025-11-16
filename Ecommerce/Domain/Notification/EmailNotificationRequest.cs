using Ecommerce.Domain.Notification.Contract;

namespace Ecommerce.Domain.Notification;

public class EmailNotificationRequest : NotificationRequest
{
    public string? From { get; set; }
    
    public string To { get; set; }
    
    public string Subject { get; set; }
}