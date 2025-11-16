using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Notification.Email;

public class EmailNotificationService(
    SmtpClient smtpClient, 
    IOptions<EmailConfig> emailConfig,
    ILogger<EmailNotificationService> logger)
{
    
    public Task NotifyAsync(EmailPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        logger.LogInformation("Trying to send email notification.");
        var message = new MailMessage(
            from: emailConfig.Value.FromEmail, 
            to: payload.To) 
        {
            Subject = payload.Subject,
            Body = payload.Message,
        };
        smtpClient.SendAsync(message, null);
        logger.LogInformation("Email notification has been sent.");
        return Task.CompletedTask;
    }
  
}