using System.Net.Mail;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;

namespace EcommerceLib.Messaging;

public class EmailService(
    SmtpClient smtpClient,
    IOptions<EmailConfig> emailConfig) : IEmailSender, IEmailService
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var mailMessage = new MailMessage(
            from: emailConfig.Value.FromEmail, 
            to: email) 
        {
            Subject = subject,
            Body = htmlMessage,
        };
        smtpClient.SendAsync(mailMessage, null);
        return Task.CompletedTask;
    }

    public Task SendAsync(string to, string subject, string message)
    {
        return SendEmailAsync(to, subject, message);   
    }
}