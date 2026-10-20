using Inventory.Features.Inventory.EventHandlers;
using Inventory.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInventoryInfrastructure(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<InventoryDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddMassTransit(x =>
        {
            x.AddConsumer<ProductCreatedIntegrationEventConsumer>();
            x.AddConsumer<ProductDeletedIntegrationEventConsumer>();

            // x.UsingRabbitMq((context, cfg) =>
            // {
            //     var host = configuration["RabbitMq:Host"] ??
            //                throw new InvalidOperationException("RabbitMq:Host missing");
            //     cfg.Host(host, "/", h =>
            //     {
            //         h.Username(configuration["RabbitMq:Username"] ?? "guest");
            //         h.Password(configuration["RabbitMq:Password"] ?? "guest");
            //     });
            //
            //     // THIS IS THE MAGIC LINE
            //     // It auto-provisions queues and binds them to Catalog's exchanges!
            //     cfg.ConfigureEndpoints(context);
            // });
        });
        return services;
    }
}