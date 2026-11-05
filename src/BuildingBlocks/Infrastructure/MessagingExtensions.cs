using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure;

public static class MessagingExtensions
{
    public static IServiceCollection AddMessaging(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            // Note: We do NOT add consumers here. The modules do that!
            // We only configure the bus infrastructure.
            
            x.UsingRabbitMq((context, cfg) =>
            {
                var host = configuration["RabbitMq:Host"]
                           ?? throw new InvalidOperationException("RabbitMq:Host is missing from configuration.");

                var username = configuration["RabbitMq:Username"] ?? "guest";
                var password = configuration["RabbitMq:Password"] ?? "guest";
                var port = ushort.TryParse(configuration["RabbitMq:Port"], out var p) ? p : (ushort)5672;

                cfg.Host(host, port, "/", h =>
                {
                    h.Username(username);
                    h.Password(password);
                });

                // This will auto-provision queues for ALL consumers
                // registered by ALL modules before this is called.
                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}