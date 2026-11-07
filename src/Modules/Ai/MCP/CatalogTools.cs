using System.ComponentModel;
using Catalog.Features.Products.SemanticSearch;
using MediatR;
using ModelContextProtocol.Server;

namespace Ai.Mcp;

[McpServerToolType]
public sealed class CatalogTools(ISender sender)
{
    [McpServerTool]
    [Description("Semantically search the product catalog. Use for natural language queries like 'cheap headphones for flights'.")]
    public async Task<string> SearchProducts(
        [Description("The search query")] string query,
        [Description("Max results to return, defaults to 5")] int limit = 5,
        CancellationToken ct = default)
    {
        // Uses MediatR to route to Catalog's handler — clean, no direct reference
        var results = await sender.Send(new SemanticSearchQuery(query, limit), ct);

        if (results.Count == 0) return "No matching products found.";

        return string.Join("\n", results.Select(r =>
            $"• {r.Name} — KES {r.Price:N0} (match: {r.SimilarityScore:P0})\n  {r.Description}"));
    }
}