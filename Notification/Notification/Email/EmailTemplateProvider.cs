using System.Text.Json;

namespace Notification.Email;


public class EmailTemplateProvider : IEmailTemplateProvider
{
    private readonly Dictionary<string, EmailTemplate> _templates;

    public EmailTemplateProvider(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var templates = JsonSerializer.Deserialize<List<EmailTemplate>>(json);
        if (templates == null) throw new ArgumentNullException(nameof(templates));
        _templates = templates.ToDictionary(t => t.TemplateName, t => t);
    }

    public EmailTemplate? GetTemplate(string templateName)
    {
        return _templates.GetValueOrDefault(templateName);
    }
}