namespace Notification.Email;

public interface IEmailTemplateProvider
{
    EmailTemplate? GetTemplate(string templateName);
}