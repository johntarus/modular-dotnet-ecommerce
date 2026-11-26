using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orders.Features;
using Orders.Infrastructure;

namespace Orders;

public static class OrdersModule
{
    public static IServiceCollection AddOrders(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddOrdersInfrastructure(config);

        return services;
    }

    public static IEndpointRouteBuilder MapOrders(
        this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api")
            .WithTags("Orders");
        group.MapOrdersEndpoints();

        return app;
    }
}