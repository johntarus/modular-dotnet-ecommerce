using Inventory.Features.Inventory;
using Inventory.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory;

public static class InventoryModule
{
    public static IServiceCollection AddInventory(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddInventoryInfrastructure(config);

        return services;
    }

    public static IEndpointRouteBuilder MapInventory(
        this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/inventory")
            .WithTags("Inventory");
        group.MapInventoryEndpoints();

        return app;
    }
}