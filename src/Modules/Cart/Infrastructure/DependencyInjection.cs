using Cart.Infrastructure.Persistence;
using Inventory.Contracts.Grpc;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cart.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCartInfrastructure(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<CartDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddMassTransit(x =>
        {
            x.AddEntityFrameworkOutbox<CartDbContext>(o =>
            {
                o.UsePostgres();
                o.UseBusOutbox();
            });
        });
        services.AddGrpcClient<InventoryGrpc.InventoryGrpcClient>(o =>
        {
            o.Address = new Uri("http://localhost:8080");
        });
        return services;
    }
}