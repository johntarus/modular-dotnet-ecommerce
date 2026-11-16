using Customers.Features.Addresses;
using Customers.Features.Customers;
using Customers.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Customers;

public static class CustomersModule
{
    public static IServiceCollection AddCustomers(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCustomersInfrastructure(configuration);

        return services;
    }

    public static IEndpointRouteBuilder MapCustomers(
        this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/customers")
            .WithTags("Customers");

        group.MapCustomerEndpoints();
        group.MapAddressEndpoints();

        return app;
    }
}