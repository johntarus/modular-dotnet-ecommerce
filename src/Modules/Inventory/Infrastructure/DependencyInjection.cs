using Catalog.Contracts.Events;
using Confluent.Kafka;
using Inventory.Features.Inventory.EventHandlers;
using Inventory.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInventoryInfrastructure(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<InventoryDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddMassTransit(x =>
        {
            //RabbitMq
            // x.AddConsumer<ProductCreatedIntegrationEventConsumer>();
            x.AddConsumer<ProductDeletedIntegrationEventConsumer>();
            x.AddEntityFrameworkOutbox<InventoryDbContext>(o =>
            {
                o.UsePostgres();
            });
            
            // Kafka consumer
            x.AddRider(rider =>
            {
                rider.AddConsumer<ProductCreatedIntegrationEventConsumer>();

                // Registers ITopicProducer<ProductCreatedIntegrationEvent>, used by Catalog's
                // ProductCreatedKafkaEventHandler. Only one Kafka rider can exist per bus, so the
                // producer is registered here instead of in a second AddRider call.
                rider.AddProducer<ProductCreatedIntegrationEvent>("product-created-topic");

                rider.UsingKafka((context, kafka) =>
                {
                    var bootstrapServers =
                        configuration["Kafka:BootstrapServers"]
                        ?? throw new InvalidOperationException(
                            "Kafka:BootstrapServers is missing.");

                    kafka.Host(bootstrapServers);

                    kafka.TopicEndpoint<ProductCreatedIntegrationEvent>(
                        "product-created-topic",
                        "inventory-service",
                        endpoint =>
                        {
                            endpoint.ConfigureConsumer<
                                ProductCreatedIntegrationEventConsumer>(context);

                            endpoint.AutoOffsetReset = AutoOffsetReset.Earliest;

                            // Consumers don't auto-create topics; without this the endpoint faults
                            // with "Unknown topic or partition" until something produces to it.
                            endpoint.CreateIfMissing(t =>
                            {
                                t.NumPartitions = 3;
                                t.ReplicationFactor = 1;
                            });
                        });
                });
            });
        });

        return services;
    }
}