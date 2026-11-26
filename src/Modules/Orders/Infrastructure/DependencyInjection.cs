using BuildingBlocks.EventBus;
using Catalog.Infrastructure.Messaging;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orders.Infrastructure.Persistence;

namespace Orders.Infrastructure;

internal static class DependencyInjection
{
    public static IServiceCollection AddOrdersInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<OrdersDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IEventBus, EventBus>();

        services.AddMassTransit(x =>
        {
            // x.AddConsumer<ProductCreatedIntegrationEventConsumer>();
            // x.AddConsumer<ProductDeletedIntegrationEventConsumer>();
            x.AddEntityFrameworkOutbox<OrdersDbContext>(o =>
            {
                o.UsePostgres();
                o.UseBusOutbox();
            });
        });

        return services;
    }
}