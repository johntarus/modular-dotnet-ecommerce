using Ai.Contracts;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Ai.IntegrationTests.Infrastructure;

/// <summary>
/// One real Ollama container for the whole run, serving the same models docker-compose uses.
/// Models live in the named volume "ecommerce-tests-ollama", separate from the dev volume, so they
/// are downloaded once and reused across runs. A missing model is pulled on first use.
/// </summary>
public sealed class OllamaFixture : IAsyncLifetime
{
    public const string EmbeddingModel = "mxbai-embed-large";
    public const string ChatModel = "qwen2.5:3b";
    private const int Port = 11434;

    private readonly IContainer _container = new ContainerBuilder("ollama/ollama:latest")
        .WithPortBinding(Port, assignRandomHostPort: true)
        .WithVolumeMount("ecommerce-tests-ollama", "/root/.ollama")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(Port).ForPath("/api/version")))
        .Build();

    public string BaseUrl => $"http://{_container.Hostname}:{_container.GetMappedPublicPort(Port)}";

    public AiOptions Options => new()
    {
        OllamaBaseUrl = BaseUrl,
        OllamaEmbeddingModel = EmbeddingModel,
        OllamaChatModel = ChatModel,
    };

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        foreach (var model in new[] { EmbeddingModel, ChatModel })
        {
            if ((await _container.ExecAsync(["ollama", "show", model])).ExitCode == 0) continue;

            var pull = await _container.ExecAsync(["ollama", "pull", model]);
            if (pull.ExitCode != 0)
                throw new InvalidOperationException($"Could not pull Ollama model '{model}': {pull.Stderr}");
        }
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

/// <summary>
/// Every test that talks to Ollama joins this collection: they share one container and run one at a
/// time. Loading the chat model twice at once (e.g. two containers, or parallel classes) can exhaust
/// Docker's memory, and Ollama then answers with 500s.
/// </summary>
[CollectionDefinition(Name)]
public sealed class OllamaCollection : ICollectionFixture<OllamaFixture>
{
    public const string Name = "Ollama";
}
