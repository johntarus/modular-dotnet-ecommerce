using Catalog.Contracts.Events;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistence;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Inventory.Features.Inventory.EventHandlers;

public class ProductCreatedIntegrationEventConsumer(
    InventoryDbContext _context,
    ILogger<ProductCreatedIntegrationEventConsumer> logger) : IConsumer<ProductCreatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<ProductCreatedIntegrationEvent> context)
    {
        logger.LogInformation("Received ProductCreated event for Product {ProductId}", context.Message.ProductId);
        var item = InventoryItem.Create(context.Message.ProductId);
        _context.InventoryItems.Add(item);
        await _context.SaveChangesAsync();
    }
}