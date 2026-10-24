using Cart.Features.Carts;
using Cart.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cart;

public static class CartModule
{
    public static IServiceCollection AddCart(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCartInfrastructure(configuration);
        return services;
    }

    public static IEndpointRouteBuilder MapCart(this IEndpointRouteBuilder app)
    {
        app.MapCartEndpoints();
        return app;
    }
}