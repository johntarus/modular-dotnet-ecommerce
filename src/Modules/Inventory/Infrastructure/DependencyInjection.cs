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
            x.AddEntityFrameworkOutbox<InventoryDbContext>(o =>
            {
                o.UsePostgres();
                o.UseBusOutbox();
            });
        });

        services.AddMassTransit(x =>
        {
            x.AddConsumer<ProductCreatedIntegrationEventConsumer>();
            x.AddConsumer<ProductDeletedIntegrationEventConsumer>();
        });
        return services;
    }
}