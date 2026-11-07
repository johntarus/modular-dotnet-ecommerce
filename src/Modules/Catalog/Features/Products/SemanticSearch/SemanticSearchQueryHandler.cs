using Ai.Contracts;
using Catalog.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace Catalog.Features.Products.SemanticSearch;

internal sealed class SemanticSearchQueryHandler(
    CatalogDbContext context,
    IEmbeddingService embeddingService)
    : IRequestHandler<SemanticSearchQuery, List<SemanticSearchResult>>
{
    public async Task<List<SemanticSearchResult>> Handle(
        SemanticSearchQuery request, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Query);

        var limit = Math.Clamp(request.Limit, 1, 100);

        var prefixedQuery = $"search_query: {request.Query}";
        
        // 1. Embed the user's query
        var embeddingResponse = await embeddingService.EmbedAsync(prefixedQuery, ct);
        var queryVector = new Vector(embeddingResponse.Vector);

        // 2. Search via pgvector cosine distance
        //    Cosine distance: 0 = identical, 2 = opposite.
        //    Similarity = 1 - distance.
        var results = await context.Products
            .AsNoTracking()
            .Where(p => p.Embedding != null)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.Price,
                p.CategoryId,
                Distance = p.Embedding!.CosineDistance(queryVector)
            })
            .OrderBy(r => r.Distance)
            .Take(limit)
            .Select(r => new SemanticSearchResult(
                r.Id,
                r.Name,
                r.Description,
                r.Price,
                r.CategoryId,
                SimilarityScore: 1 - r.Distance))
            .ToListAsync(ct);

        // 3. Optional threshold filter
        if (request.MinSimilarity > 0)
            results = results.Where(r => r.SimilarityScore >= request.MinSimilarity).ToList();

        return results;
    }
}