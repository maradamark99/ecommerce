using Ecommerce.Domain.Notification.Contract;

namespace Ecommerce.Domain.Notification;

public static class NotificationServiceExtensions
{
    public static IServiceCollection AddNotificationServices(this IServiceCollection services)
    {
        services.AddSingleton<IEmailService, EmailNotificationHandler>();
        services.AddSingleton<INotificationHandlerFactory, NotificationHandlerFactory>();
        return services;
    }
}