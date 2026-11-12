using System.Diagnostics;
using Ai.Contracts;
using Ai.Contracts.Models;
using BuildingBlocks.Infrastructure.Observability;

namespace Ai.Infrastructure.Ollama;

internal sealed class MeteredChatCompletionService(IChatCompletionService inner) : IChatCompletionService
{
    public async Task<ChatResponse> CompleteAsync(
        IReadOnlyList<ChatMessage> messages, CancellationToken ct = default)
    {
        var feature = Activity.Current?.GetTagItem("ai.feature")?.ToString() ?? "unknown";

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await inner.CompleteAsync(messages, ct);
            stopwatch.Stop();

            // Build TagList AFTER we have the actual model
            var tags = new TagList
            {
                { "feature", feature },
                { "model", response.Model }
            };

            AiMetrics.InputTokens.Add(response.InputTokens, tags);
            AiMetrics.OutputTokens.Add(response.OutputTokens, tags);
            AiMetrics.RequestCount.Add(1, tags);
            AiMetrics.RequestDuration.Record(stopwatch.Elapsed.TotalMilliseconds, tags);

            // Tag the span for Tempo correlation
            Activity.Current?.SetTag("ai.tokens.input", response.InputTokens);
            Activity.Current?.SetTag("ai.tokens.output", response.OutputTokens);
            Activity.Current?.SetTag("ai.model", response.Model);

            return response;
        }
        catch (Exception)
        {
            stopwatch.Stop();

            // In the failure path, model is unknown
            var tags = new TagList
            {
                { "feature", feature },
                { "model", "unknown" }
            };

            AiMetrics.RequestFailures.Add(1, tags);
            throw;
        }
    }
}