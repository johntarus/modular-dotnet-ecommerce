using Ai.Contracts;
using Catalog.Contracts.Events;
using Catalog.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pgvector;

namespace Catalog.Features.Products.EventHandlers;

internal sealed class GenerateEmbeddingConsumer(
    CatalogDbContext context,
    IEmbeddingService embeddingService,
    ILogger<GenerateEmbeddingConsumer> logger)
    : IConsumer<ProductCreatedIntegrationEvent>, IConsumer<ProductUpdatedIntegrationEvent>
{
    public Task Consume(ConsumeContext<ProductCreatedIntegrationEvent> context)
    {
        return GenerateEmbeddingAsync(context.Message.ProductId, context.CancellationToken);
    }

    public Task Consume(ConsumeContext<ProductUpdatedIntegrationEvent> context)
    {
        return GenerateEmbeddingAsync(context.Message.ProductId, context.CancellationToken);
    }

    public async Task GenerateEmbeddingAsync(Guid productId, CancellationToken ct)
    {
        var product = await context.Products
            .FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted, ct);

        if (product is null)
        {
            logger.LogWarning("Product with id {ProductId} not found", productId);
            return;
        }

        // Idempotency: skip if embedding is already fresh
        if (product.Embedding is not null && product.EmbeddingGeneratedAt >= product.UpdatedAt)
        {
            logger.LogInformation("Embedding for product {ProductId} is fresh; skipping", productId);
            return;
        }

        var text = product.GetEmbeddingText();
        var response = await embeddingService.EmbedAsync(text, ct);

        product.SetEmbedding(new Vector(response.Vector));
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Generated {Dimensions}-dim Embedding for product {ProductId}", 
            response.Dimensions, productId);
    }
}