namespace Ecommerce.Domain.Notification;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string message);
}