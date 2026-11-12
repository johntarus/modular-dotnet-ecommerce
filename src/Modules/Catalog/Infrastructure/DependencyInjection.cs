using Ai.Contracts;
using BuildingBlocks.EventBus;
using Catalog.Contracts;
using Catalog.Infrastructure.Messaging;
using Catalog.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Infrastructure;

internal static class DependencyInjection
{
    public static IServiceCollection AddCatalogInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CatalogDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                o => o.UseVector()));

        services.AddScoped<IEventBus, EventBus>();
        services.AddScoped<ICatalogApi, CatalogApi>();
        // services.AddScoped<IChatCompletionService, IChatCompletionService>();

        services.AddMassTransit(x =>
        {
            x.AddEntityFrameworkOutbox<CatalogDbContext>(o =>
            {
                o.UsePostgres();
                o.UseBusOutbox();
            });
        });

        return services;
    }
}