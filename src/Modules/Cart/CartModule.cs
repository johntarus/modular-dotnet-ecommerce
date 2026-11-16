using Cart.Features.Carts;
using Cart.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cart;

public static class CartModule
{
    public static IServiceCollection AddCart(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCartInfrastructure(configuration);

        return services;
    }

    public static IEndpointRouteBuilder MapCart(
        this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/cart")
            .WithTags("Cart");

        group.MapCartEndpoints();

        return app;
    }
}