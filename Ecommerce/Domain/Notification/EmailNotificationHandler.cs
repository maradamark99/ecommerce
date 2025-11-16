using System.Net.Mail;
using Ecommerce.Domain.Notification.Contract;
using Microsoft.Extensions.Options;

namespace Ecommerce.Domain.Notification;

public class EmailNotificationHandler(
    SmtpClient smtpClient,
    IOptions<EmailConfig> emailConfig) : IEmailService, INotificationHandler<EmailNotificationRequest> 
{
    public Task NotifyAsync(EmailNotificationRequest notificationRequest)
    {
        ArgumentNullException.ThrowIfNull(notificationRequest);
        var message = new MailMessage(
            from: emailConfig.Value.FromEmail, 
            to: notificationRequest.To) 
        {
            Subject = notificationRequest.Subject,
            Body = notificationRequest.Message,
        };
        smtpClient.SendAsync(message, null);
        return Task.CompletedTask;
    }

    public Task SendEmailAsync(string to, string subject, string message)
    {
        var mailMessage = new MailMessage(
            from: emailConfig.Value.FromEmail, 
            to: to) 
        {
            Subject = subject,
            Body = message,
        };
        smtpClient.SendAsync(mailMessage, null);
        return Task.CompletedTask;
    }
}