using System.Diagnostics.Metrics;
using Ai.Contracts;
using Ai.Contracts.Models;
using Ai.Infrastructure;
using BuildingBlocks.Infrastructure.Observability;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ai.IntegrationTests.Infrastructure;

/// <summary>The services exactly as the app wires them, pointed at the Ollama container.</summary>
[Collection(OllamaCollection.Name)]
public class AddAiTests(OllamaFixture ollama)
{
    private ServiceProvider Build() =>
        new ServiceCollection()
            .AddAi(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ai:OllamaBaseUrl"] = ollama.BaseUrl,
                ["Ai:OllamaEmbeddingModel"] = OllamaFixture.EmbeddingModel,
                ["Ai:OllamaChatModel"] = OllamaFixture.ChatModel,
            }).Build())
            .BuildServiceProvider();

    [Fact]
    public async Task EmbeddingService_EmbedsThroughOllama()
    {
        using var provider = Build();

        var response = await provider.GetRequiredService<IEmbeddingService>().EmbedAsync("Electric kettle");

        Assert.Equal(1024, response.Dimensions);
    }

    [Fact]
    public async Task DefaultChatService_RecordsTokenMetrics()
    {
        using var provider = Build();
        var inputTokens = 0L;
        var requests = 0L;
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == AiMetrics.MeterName) l.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
        {
            foreach (var tag in tags)
                if (tag is { Key: "model", Value: OllamaFixture.ChatModel })
                {
                    if (instrument.Name == "ai.tokens.input") Interlocked.Add(ref inputTokens, value);
                    if (instrument.Name == "ai.request.count") Interlocked.Add(ref requests, value);
                }
        });
        listener.Start();

        var response = await provider.GetRequiredService<ILlmProviderFactory>().GetChatService()
            .CompleteAsync([ChatMessage.System("Reply with one word."), ChatMessage.User("Hello")]);

        Assert.False(string.IsNullOrWhiteSpace(response.Content));
        Assert.True(Interlocked.Read(ref requests) >= 1);
        Assert.True(Interlocked.Read(ref inputTokens) >= response.InputTokens);
    }
}
