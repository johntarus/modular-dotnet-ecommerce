using Customers.Contracts.Events;
using Customers.Domain.Events;
using Customers.Features.Customers.EventHandlers;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Customers.IntegrationTests.Features;

/// <summary>Domain event → integration event on the bus (in-memory MassTransit harness, no RabbitMQ).</summary>
public class CustomerRegisteredDomainEventHandlerTests : IAsyncLifetime
{
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;

    public async Task InitializeAsync()
    {
        _provider = new ServiceCollection().AddMassTransitTestHarness().BuildServiceProvider(true);
        _harness = _provider.GetRequiredService<ITestHarness>();
        await _harness.Start();
    }

    public async Task DisposeAsync()
    {
        await _harness.Stop();
        await _provider.DisposeAsync();
    }

    [Fact]
    public async Task Handle_PublishesCustomerRegisteredIntegrationEvent()
    {
        var customerId = Guid.NewGuid();
        var before = DateTime.UtcNow;

        await new CustomerRegisteredDomainEventHandler(_harness.Bus)
            .Handle(new CustomerRegisteredDomainEvent(customerId, "jane@shop.test", "Jane"), default);

        var published = _harness.Published.Select<CustomerRegisteredIntegrationEvent>().ToList();
        var message = Assert.Single(published).Context.Message;
        Assert.Equal(customerId, message.CustomerId);
        Assert.Equal("jane@shop.test", message.Email);
        Assert.Equal("Jane", message.FirstName);
        Assert.InRange(message.RegisteredAt, before, DateTime.UtcNow);
    }

    [Fact]
    public async Task Handle_WithoutEmail_StillPublishes_WithNullEmail()
    {
        var customerId = Guid.NewGuid();

        await new CustomerRegisteredDomainEventHandler(_harness.Bus)
            .Handle(new CustomerRegisteredDomainEvent(customerId, null, null), default);

        var message = Assert.Single(_harness.Published.Select<CustomerRegisteredIntegrationEvent>().ToList()).Context.Message;
        Assert.Equal(customerId, message.CustomerId);
        Assert.Null(message.Email);
        Assert.Null(message.FirstName);
    }
}
