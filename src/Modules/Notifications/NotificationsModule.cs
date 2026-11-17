using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Features;
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
    
    public static IEndpointRouteBuilder MapNotifications(
        this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/notifications")
            .WithTags("Notifications");
        group.MapNotificationEndpoints();

        return app;
    }
}
