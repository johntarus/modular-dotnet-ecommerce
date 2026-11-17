
using Catalog.Features.Categories;
using Catalog.Features.Products;
using Catalog.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog;

public static class CatalogModule
{
    public static IServiceCollection AddCatalog(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddCatalogInfrastructure(config);

        return services;
    }

    public static IEndpointRouteBuilder MapCatalog(
        this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/catalog")
            .WithTags("Catalog");
            // .WithGroupName("catalog");

        group.MapProductEndpoints();
        group.MapCategoryEndpoints();

        return app;
    }
}