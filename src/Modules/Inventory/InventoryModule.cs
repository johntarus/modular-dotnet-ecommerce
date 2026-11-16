using Inventory.Features.Inventory;
using Inventory.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Inventory;

public static class InventoryModule
{
    public static IServiceCollection AddInventory(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddInventoryInfrastructure(config);
        services.ConfigureOptions<ConfigureInventorySwaggerOptions>();

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
    
    internal sealed class ConfigureInventorySwaggerOptions : IConfigureOptions<SwaggerGenOptions>
    {
        public void Configure(SwaggerGenOptions options)
        {
            options.SwaggerDoc("inventory", new OpenApiInfo
            {
                Title = "Inventory Module",
                Version = "v1"
            });
        }
    }
}