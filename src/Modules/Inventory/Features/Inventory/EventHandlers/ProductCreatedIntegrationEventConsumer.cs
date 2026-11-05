using Catalog.Contracts.Events;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Inventory.Features.Inventory.EventHandlers;

public class ProductCreatedIntegrationEventConsumer(
    InventoryDbContext _context,
    ILogger<ProductCreatedIntegrationEventConsumer> logger) : IConsumer<ProductCreatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<ProductCreatedIntegrationEvent> context)
    {
        logger.LogInformation("Received ProductCreated event for Product {ProductId}", context.Message.ProductId);

        // RabbitMQ delivers at least once: a redelivered event must not try to insert the same key again.
        // IgnoreQueryFilters so a soft-deleted record also counts as "already handled".
        var exists = await _context.InventoryItems
            .IgnoreQueryFilters()
            .AnyAsync(i => i.ProductId == context.Message.ProductId, context.CancellationToken);
        if (exists)
        {
            logger.LogInformation("Inventory record for Product {ProductId} already exists; skipping duplicate event",
                context.Message.ProductId);
            return;
        }

        var item = InventoryItem.Create(context.Message.ProductId);
        _context.InventoryItems.Add(item);
        await _context.SaveChangesAsync();
    }
}