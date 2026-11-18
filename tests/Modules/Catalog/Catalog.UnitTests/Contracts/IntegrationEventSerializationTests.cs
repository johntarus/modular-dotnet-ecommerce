using System.Text.Json;
using BuildingBlocks.Contracts.Events;
using Catalog.Contracts.Events;
using MassTransit.Serialization;

namespace Catalog.UnitTests.Contracts;

/// <summary>
/// Integration events cross RabbitMQ as JSON. The consumer must see the same EventId the publisher
/// generated, otherwise ids can't be used to correlate or de-duplicate deliveries.
/// </summary>
public class IntegrationEventSerializationTests
{
    public static TheoryData<IIntegrationEvent> Events => new()
    {
        new ProductCreatedIntegrationEvent(Guid.NewGuid(), "Lamp", 19.99m, Guid.NewGuid(), DateTime.UtcNow),
        new ProductUpdatedIntegrationEvent(Guid.NewGuid(), "Lamp", "Desk lamp", Guid.NewGuid(), DateTime.UtcNow),
        new ProductDeletedIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow),
        new ProductPriceChangedIntegrationEvent(Guid.NewGuid(), 10m, 12m, DateTime.UtcNow),
    };

    [Theory]
    [MemberData(nameof(Events))]
    public void HasAnId(IIntegrationEvent evt)
    {
        Assert.NotEqual(Guid.Empty, evt.EventId);
        Assert.NotEqual(default, evt.OccurredAt);
    }

    [Theory]
    [MemberData(nameof(Events))]
    public void KeepsIdAndTimestamp_ThroughMassTransitSerialization(IIntegrationEvent sent)
    {
        var type = sent.GetType();

        var json = JsonSerializer.Serialize(sent, type, SystemTextJsonMessageSerializer.Options);
        var received = (IIntegrationEvent)JsonSerializer.Deserialize(json, type, SystemTextJsonMessageSerializer.Options)!;

        Assert.Equal(sent.EventId, received.EventId);
        Assert.Equal(sent.OccurredAt, received.OccurredAt);
        Assert.Equal(sent, received);
    }
}
