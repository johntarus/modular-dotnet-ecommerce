namespace Catalog.Features.Products.SemanticSearch;

public sealed record SemanticSearchRequest(string Query, int Limit = 20);