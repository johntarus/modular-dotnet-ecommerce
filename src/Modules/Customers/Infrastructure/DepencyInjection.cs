using Customers.Contracts;
using Customers.Infrastructure.Contracts;
using Customers.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Customers.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCustomersInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<CustomersDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        
        services.AddScoped<ICustomersModuleApi, CustomersModuleApi>();
        
        // No EF outbox here: MassTransit allows a single UseBusOutbox() DbContext per bus and Catalog already owns it.
        // A second one replaces Catalog's, so Catalog's events would be written to a context that is never saved.
        // Customers publishes straight to the bus after SaveChanges instead (see CustomerRegisteredDomainEventHandler).

        return services;
    }
}