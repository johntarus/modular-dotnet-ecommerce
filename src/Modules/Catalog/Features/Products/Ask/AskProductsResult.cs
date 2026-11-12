using Catalog.Features.Products.SemanticSearch;

namespace Catalog.Features.Products.Ask;

public record AskProductsResult(string Answer, 
    List<SemanticSearchResult> Sources,   // for auditability
    int InputTokens,
    int OutputTokens);