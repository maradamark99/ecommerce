using System.Net.Mail;
using Ecommerce.Domain.Notification.Contract;
using Microsoft.Extensions.Options;

namespace Ecommerce.Domain.Notification;

public class NotificationHandlerFactory(
    IOptions<EmailConfig> emailConfig,
    SmtpClient smtpClient) : INotificationHandlerFactory
{
    public INotificationHandler<T> Create<T>() where T : NotificationRequest
    {
        if (typeof(T) == typeof(EmailNotificationRequest))
            return (INotificationHandler<T>)new EmailNotificationHandler(smtpClient, emailConfig);
        throw new NotSupportedException($"Notification request type {typeof(T).Name} is not supported.");
    }
}