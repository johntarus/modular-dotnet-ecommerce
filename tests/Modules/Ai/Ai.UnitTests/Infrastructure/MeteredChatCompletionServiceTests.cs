using System.Diagnostics;
using System.Diagnostics.Metrics;
using Ai.Contracts.Models;
using Ai.Infrastructure;
using Ai.UnitTests.Fakes;
using BuildingBlocks.Infrastructure.Observability;

namespace Ai.UnitTests.Infrastructure;

/// <summary>
/// AiMetrics instruments are static, so measurements from tests running in parallel can leak into
/// a listener. Each test tags its Activity with a unique feature name and only counts its own.
/// </summary>
public sealed class MeteredChatCompletionServiceTests : IDisposable
{
    private sealed record Measurement(string Instrument, double Value, Dictionary<string, object?> Tags);

    private readonly List<Measurement> _measurements = [];
    private readonly MeterListener _listener = new();
    private readonly string _feature = $"test-{Guid.NewGuid():N}";
    private readonly Activity _activity;

    public MeteredChatCompletionServiceTests()
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == AiMetrics.MeterName) listener.EnableMeasurementEvents(instrument);
        };
        _listener.SetMeasurementEventCallback<long>(Record);
        _listener.SetMeasurementEventCallback<double>(Record);
        _listener.Start();

        _activity = new Activity("test").Start();
        _activity.SetTag("ai.feature", _feature);
    }

    private void Record<T>(Instrument instrument, T value, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? _)
        where T : struct
    {
        var tagMap = tags.ToArray().ToDictionary(t => t.Key, t => t.Value);
        if (!Equals(tagMap.GetValueOrDefault("feature"), _feature)) return;
        lock (_measurements) _measurements.Add(new(instrument.Name, Convert.ToDouble(value), tagMap));
    }

    private Measurement[] Recorded(string instrument) => _measurements.Where(m => m.Instrument == instrument).ToArray();

    public void Dispose()
    {
        _activity.Dispose();
        _listener.Dispose();
    }

    [Fact]
    public async Task Success_RecordsTokensRequestAndDuration_TaggedWithFeatureAndModel()
    {
        var inner = new FakeChatService("hi", input: 42, output: 7, model: "qwen2.5:3b");

        var response = await new MeteredChatCompletionService(inner).CompleteAsync([ChatMessage.User("hello")]);

        Assert.Equal("hi", response.Content);
        Assert.Equal(42, Assert.Single(Recorded("ai.tokens.input")).Value);
        Assert.Equal(7, Assert.Single(Recorded("ai.tokens.output")).Value);
        Assert.Equal(1, Assert.Single(Recorded("ai.request.count")).Value);
        Assert.True(Assert.Single(Recorded("ai.request.duration")).Value >= 0);
        Assert.Empty(Recorded("ai.request.failures"));
        Assert.All(_measurements, m => Assert.Equal("qwen2.5:3b", m.Tags["model"]));
    }

    [Fact]
    public async Task Success_TagsCurrentSpanWithTokensAndModel()
    {
        var inner = new FakeChatService("hi", input: 42, output: 7, model: "qwen2.5:3b");

        await new MeteredChatCompletionService(inner).CompleteAsync([ChatMessage.User("hello")]);

        Assert.Equal(42, _activity.GetTagItem("ai.tokens.input"));
        Assert.Equal(7, _activity.GetTagItem("ai.tokens.output"));
        Assert.Equal("qwen2.5:3b", _activity.GetTagItem("ai.model"));
    }

    [Fact]
    public async Task Failure_RecordsFailureWithUnknownModel_AndRethrows()
    {
        var inner = new FakeChatService(_ => throw new HttpRequestException("ollama down"));

        var ex = await Assert.ThrowsAsync<HttpRequestException>(
            () => new MeteredChatCompletionService(inner).CompleteAsync([ChatMessage.User("hello")]));

        Assert.Equal("ollama down", ex.Message);
        var failure = Assert.Single(Recorded("ai.request.failures"));
        Assert.Equal("unknown", failure.Tags["model"]);
        Assert.Empty(Recorded("ai.request.count"));
        Assert.Empty(Recorded("ai.tokens.input"));
    }

    [Fact]
    public async Task NoCurrentActivity_TagsFeatureAsUnknown()
    {
        var seen = new List<object?>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (i, l) => { if (i.Name == "ai.request.count") l.EnableMeasurementEvents(i); };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            foreach (var t in tags) if (t.Key == "feature") lock (seen) seen.Add(t.Value);
        });
        listener.Start();

        // Run on a fresh async flow with no ambient Activity.
        await Task.Run(async () =>
        {
            Activity.Current = null;
            await new MeteredChatCompletionService(new FakeChatService()).CompleteAsync([ChatMessage.User("x")]);
        });

        Assert.Contains("unknown", seen);
    }
}
