using Ai.Contracts;
using Catalog.Domain.Events;
using Catalog.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pgvector;

namespace Catalog.Features.Products.EventHandlers;

internal sealed class ProductCreatedEmbeddingHandler(
    IServiceScopeFactory scopeFactory,
    ILogger<ProductCreatedEmbeddingHandler> logger)
    : INotificationHandler<ProductCreatedDomainEvent>,
      INotificationHandler<ProductUpdatedDomainEvent>
{
    public Task Handle(ProductCreatedDomainEvent notification, CancellationToken ct)
        => QueueEmbeddingAsync(notification.ProductId);

    public Task Handle(ProductUpdatedDomainEvent notification, CancellationToken ct)
        => QueueEmbeddingAsync(notification.ProductId);

    private Task QueueEmbeddingAsync(Guid productId)
    {
        // FIRE-AND-FORGET: return immediately so the outer SaveChangesAsync can commit.
        //    Do NOT await — the outer transaction needs to finish first.
        _ = GenerateEmbeddingInBackgroundAsync(productId);
        return Task.CompletedTask;
    }

    private async Task GenerateEmbeddingInBackgroundAsync(Guid productId)
    {
        try
        {
            // Small delay: let the outer transaction commit before we query.
            //    Without this, the product won't be visible to our fresh DbContext.
            await Task.Delay(TimeSpan.FromMilliseconds(300));

            // Fresh scope — completely separate DbContext, no recursion risk
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            var embedder = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();

            var product = await db.Products
                .FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted);

            if (product is null)
            {
                logger.LogWarning("Product {ProductId} not found; skipping embedding", productId);
                return;
            }

            // Idempotency: skip if embedding is already fresh
            if (product.Embedding is not null
                && product.EmbeddingGeneratedAt >= product.UpdatedAt)
            {
                logger.LogDebug("Embedding for {ProductId} is fresh; skipping", productId);
                return;
            }

            var text = product.GetEmbeddingText();
            var response = await embedder.EmbedAsync(text);

            product.SetEmbedding(new Vector(response.Vector));
            await db.SaveChangesAsync();

            logger.LogInformation(
                "Generated {Dimensions}-dim embedding for {ProductId}",
                response.Dimensions, productId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate embedding for {ProductId}", productId);
            // Swallow — a failed embedding must never break product creation
        }
    }
}