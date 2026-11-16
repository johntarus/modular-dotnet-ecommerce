using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Infrastructure;

namespace Notifications;

public static class NotificationsModule
{
    public static IServiceCollection AddNotifications(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddNotificationsInfrastructure(config);
        return services;
    }
}
