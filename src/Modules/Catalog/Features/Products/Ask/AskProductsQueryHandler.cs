using System.Diagnostics;
using Ai.Contracts;
using Ai.Contracts.Models;
using BuildingBlocks.Infrastructure.CurrentUser;
using Catalog.Features.Products.SemanticSearch;
using Catalog.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace Catalog.Features.Products.Ask;

internal sealed class AskProductsQueryHandler(
    CatalogDbContext context,
    IEmbeddingService embeddingService,
    ILlmProviderFactory llmProviderFactory,
    ICurrentUser currentUser) : IRequestHandler<AskProductsQuery, AskProductsResult>
{
    private const double MinRelevance = 0.55;
    private const int TopK = 5;

    public async Task<AskProductsResult> Handle(
        AskProductsQuery request, CancellationToken ct
    )
    {
        // Tag the current Activity — the meter picks this up
        Activity.Current?.SetTag("ai.feature", "product-ask");
        Activity.Current?.SetTag("user.id", currentUser.UserId.ToString() ?? "anonymous");
        // 1. RETRIEVE — same semantic search, but filtered
        var queryVector = new Vector((await embeddingService.EmbedAsync(
            $"Represent this sentence for searching relevant passages: {request.Question}", ct)).Vector);

        var products = await context.Products.AsNoTracking()
            .Where(p => p.Embedding != null && !p.IsDeleted)
            .Select(p => new
            {
                p.Id, p.Name, p.Description, p.Price, p.CategoryId,
                Distance = p.Embedding!.CosineDistance(queryVector)
            })
            .Where(r => (1 - r.Distance) >= MinRelevance) // ← filter before RAG
            .OrderBy(r => r.Distance)
            .Take(TopK)
            .ToListAsync(ct);

        // 2. If nothing matched, return an honest answer
        if (products.Count == 0)
        {
            return new AskProductsResult(
                "I couldn't find any products matching your question. Try rephrasing, or browse the catalog.",
                new List<SemanticSearchResult>(), 0, 0);
        }

        // 3. AUGMENT — build the prompt
        var contextText = string.Join("\n\n", products.Select((p, i) =>
            $"[{i + 1}] {p.Name} — {p.Description} — KES {p.Price:N0}"));

        var prompt = $"""
                      You are a shopping assistant. Your job is to RECOMMEND products, 
                      not to list them.

                      Below are the only products available. Each has a name, description, and price.

                      PRODUCTS:
                      {contextText}

                      RULES:
                      1. Write a natural recommendation in 2-3 sentences.
                      2. Do NOT copy the product descriptions verbatim.
                      3. Mention products by NAME and PRICE only.
                      4. If two products are relevant, mention both — the better match first.
                      5. Do NOT use bullet points or numbered lists. Write in prose.
                      6. Do NOT say "here are some products" or "based on the products" — just answer.

                      QUESTION: {request.Question}

                      RECOMMENDATION:
                      """;

        // STEP 4: GENERATE (Dynamically select Ollama or OpenAI)
        // The Factory resolves the correct implementation based on the request
        var chatService = llmProviderFactory.GetChatService(request.providerName);
        var response = await chatService.CompleteAsync(new[]
        {
            ChatMessage.System(
                "You are a shopping assistant that recommends products in natural prose. " +
                "Never copy product descriptions verbatim. " +
                "Never use bullet points. " +
                "Always be specific about names and prices."),
            ChatMessage.User(prompt)
        }, ct);

        return new AskProductsResult(
            response.Content,
            products.Select(p => new SemanticSearchResult(
                p.Id, p.Name, p.Description, p.Price, p.CategoryId,
                SimilarityScore: 1 - p.Distance)).ToList(),
            response.InputTokens,
            response.OutputTokens);
    }
}