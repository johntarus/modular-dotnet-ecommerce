using BuildingBlocks.Contracts.Events;
using BuildingBlocks.EventBus;
using Catalog.Contracts.Events;
using Catalog.Domain.Events;
using Catalog.Features.Products.EventHandlers;

namespace Catalog.UnitTests.Features;

/// <summary>
/// The handlers turn domain events into integration events. CatalogDbContext runs them just
/// before saving, so each publish must be awaited to land in the outbox in the same transaction.
/// </summary>
public class DomainEventHandlerTests
{
    /// <summary>
    /// An event bus that finishes publishing only after a delay. A handler that forgets to
    /// await PublishAsync returns before anything is recorded.
    /// </summary>
    private sealed class SlowEventBus : IEventBus
    {
        public List<IIntegrationEvent> Published { get; } = [];

        public async Task PublishAsync<T>(T integrationEvent, CancellationToken ct = default) where T : IIntegrationEvent
        {
            await Task.Delay(100, ct);
            Published.Add(integrationEvent);
        }
    }

    [Fact]
    public async Task ProductCreated_PublishesIntegrationEvent_BeforeReturning()
    {
        var bus = new SlowEventBus();
        var domainEvent = new ProductCreatedDomainEvent(Guid.NewGuid(), "Mug", 12m, Guid.NewGuid());

        await new ProductCreatedDomainEventHandler(bus).Handle(domainEvent, CancellationToken.None);

        var published = Assert.IsType<ProductCreatedIntegrationEvent>(Assert.Single(bus.Published));
        Assert.Equal(domainEvent.ProductId, published.ProductId);
        Assert.Equal("Mug", published.Name);
        Assert.Equal(12m, published.Price);
        Assert.Equal(domainEvent.CategoryId, published.CategoryId);
        Assert.Equal(domainEvent.OccurredAt, published.OccurredAt);
    }

    [Fact]
    public async Task ProductDeleted_PublishesIntegrationEvent_BeforeReturning()
    {
        var bus = new SlowEventBus();
        var domainEvent = new ProductDeletedDomainEvent(Guid.NewGuid());

        await new ProductDeletedDomainEventHandler(bus).Handle(domainEvent, CancellationToken.None);

        var published = Assert.IsType<ProductDeletedIntegrationEvent>(Assert.Single(bus.Published));
        Assert.Equal(domainEvent.ProductId, published.ProductId);
    }

    [Fact]
    public async Task ProductUpdated_PublishesIntegrationEvent_BeforeReturning()
    {
        var bus = new SlowEventBus();
        var domainEvent = new ProductUpdatedDomainEvent(Guid.NewGuid(), "Big mug", "Bigger", Guid.NewGuid());

        await new ProductUpdatedDomainEventHandler(bus).Handle(domainEvent, CancellationToken.None);

        var published = Assert.IsType<ProductUpdatedIntegrationEvent>(Assert.Single(bus.Published));
        Assert.Equal(domainEvent.ProductId, published.ProductId);
        Assert.Equal("Big mug", published.Name);
        Assert.Equal("Bigger", published.Description);
        Assert.Equal(domainEvent.CategoryId, published.CategoryId);
    }

    [Fact]
    public async Task ProductPriceChanged_PublishesIntegrationEvent_BeforeReturning()
    {
        var bus = new SlowEventBus();
        var domainEvent = new ProductPriceChangedDomainEvent(Guid.NewGuid(), 10m, 15m);

        await new ProductPriceChangedDomainEventHandler(bus).Handle(domainEvent, CancellationToken.None);

        var published = Assert.IsType<ProductPriceChangedIntegrationEvent>(Assert.Single(bus.Published));
        Assert.Equal(10m, published.OldPrice);
        Assert.Equal(15m, published.NewPrice);
    }
}
