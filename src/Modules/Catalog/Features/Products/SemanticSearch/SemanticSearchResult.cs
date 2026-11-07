namespace Catalog.Features.Products.SemanticSearch;

public sealed record SemanticSearchResult(
    Guid ProductId,
    string Name,
    string Description,
    decimal Price,
    Guid CategoryId,
    double SimilarityScore);