using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Notification.Clients;
using Notification.Consumers;
using Notification.Email;

namespace Notification;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.AddJsonFile("appsettings.Development.json");
        builder.Services.AddControllers();
        builder.Services.AddMemoryCache();
        builder.Services.Configure<OrderEventConsumerConfig>(builder.Configuration.GetSection(nameof(OrderEventConsumerConfig)));
        builder.Services.Configure<ShippingEventConsumerConfig>(builder.Configuration.GetSection(nameof(ShippingEventConsumerConfig)));
        builder.Services.Configure<WishlistEventConsumerConfig>(builder.Configuration.GetSection(nameof(WishlistEventConsumerConfig)));
        builder.Services.Configure<ProfileClientConfig>(builder.Configuration.GetSection(nameof(ProfileClientConfig)));
        builder.Services.Configure<EmailConfig>(builder.Configuration.GetSection(nameof(EmailConfig)));   
        builder.Services.AddSingleton<SmtpClient>(sp =>
        {
            var emailOptions = sp.GetRequiredService<IOptions<EmailConfig>>().Value;

            var client = new SmtpClient(emailOptions.Address, emailOptions.Port)
            {
                EnableSsl = emailOptions.UseSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false
            };

            if (!string.IsNullOrWhiteSpace(emailOptions.Username))
            {
                client.Credentials = new NetworkCredential(emailOptions.Username, emailOptions.Password);
            }

            return client;
        });
        builder.Services.AddSingleton<IEmailTemplateProvider>(new EmailTemplateProvider("Email/email-templates.json"));
        builder.Services.AddSingleton<IEmailTemplateMapper, EmailTemplateMapper>();
        builder.Services.AddSingleton<EmailNotificationService>();
        builder.Services.AddHttpClient<IProfileClient, ProfileClient>();
        builder.Services.AddHostedService<OrderEventConsumerService>();
        builder.Services.AddHostedService<ShippingEventConsumerService>();
        builder.Services.AddHostedService<WishlistEventConsumerService>();

        var app = builder.Build();

        app.UseExceptionHandler(_ => { });
        app.UseHttpsRedirection();
        app.UseAuthorization();

        app.MapControllers();
        await app.RunAsync();
    }
}