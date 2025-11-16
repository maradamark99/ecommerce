namespace EcommerceLib.Messaging;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string message);
}