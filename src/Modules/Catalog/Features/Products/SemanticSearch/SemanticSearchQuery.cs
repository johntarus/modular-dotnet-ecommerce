using MediatR;

namespace Catalog.Features.Products.SemanticSearch;

public sealed record SemanticSearchQuery(
    string Query,
    int Limit = 20,
    double MinSimilarity = 0.0) : IRequest<List<SemanticSearchResult>>;
    