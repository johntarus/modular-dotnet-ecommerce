using BuildingBlocks.EventBus;
using BuildingBlocks.Infrastructure;
using Catalog.Infrastructure.Messaging;
using Catalog.Infrastructure.Persistence;
using FluentValidation;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Infrastructure;

internal static class DependencyInjection
{
    public static IServiceCollection AddCatalogInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CatalogDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IEventBus, EventBus>();
        services.AddMassTransit(x =>
        {
            x.AddEntityFrameworkOutbox<CatalogDbContext>(o =>
            {
                o.UsePostgres();
                o.UseBusOutbox();
            });
        });

        // MediatR handlers, pipeline behaviors and validators for this module are
        // registered centrally in AddBuildingBlocks (Program.cs passes this assembly).

        return services;
    }
}