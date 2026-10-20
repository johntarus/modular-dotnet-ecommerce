using Inventory.Infrastructure.Persistence;
using MassTransit;
using Catalog.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Inventory.Features.Inventory.EventHandlers;

public class ProductDeletedIntegrationEventConsumer(
    InventoryDbContext _context,
    ILogger<ProductDeletedIntegrationEventConsumer> logger) : IConsumer<ProductDeletedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<ProductDeletedIntegrationEvent> context)
    {
        logger.LogInformation("Received ProductDeleted event for Product {ProductId}", context.Message.ProductId);
        var item = await _context.InventoryItems.FirstOrDefaultAsync(i => i.ProductId == context.Message.ProductId);
        if (item is not null)
        {
            item.Delete();
            await _context.SaveChangesAsync();
        }
    }
}