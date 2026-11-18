using System.Text.Json;
using Customers.Contracts.Events;
using Customers.Domain.Entities;
using Customers.Domain.Events;
using MassTransit.Serialization;

namespace Customers.UnitTests.Domain;

public class CustomerEventsTests
{
    [Fact]
    public void DomainEvent_HasAnIdAndTimestamp()
    {
        var before = DateTime.UtcNow;

        var evt = new CustomerRegisteredDomainEvent(Guid.NewGuid(), "jane@shop.test", "Jane");

        Assert.NotEqual(Guid.Empty, evt.EventId);
        Assert.InRange(evt.OccurredAt, before, DateTime.UtcNow);
    }

    [Fact]
    public void DomainEvent_RaisedByCreate_HasAnIdAndTimestamp()
    {
        var evt = Assert.Single(Customer.Create(Guid.NewGuid().ToString()).DomainEvents);

        Assert.NotEqual(Guid.Empty, evt.EventId);
        Assert.NotEqual(default, evt.OccurredAt);
    }

    [Fact]
    public void EachEvent_GetsItsOwnId()
    {
        var a = new CustomerRegisteredDomainEvent(Guid.NewGuid(), null, null);
        var b = new CustomerRegisteredDomainEvent(Guid.NewGuid(), null, null);

        Assert.NotEqual(a.EventId, b.EventId);
    }

    [Fact]
    public void IntegrationEvent_HasAnIdAndTimestamp()
    {
        var before = DateTime.UtcNow;

        var evt = new CustomerRegisteredIntegrationEvent(Guid.NewGuid(), "jane@shop.test", "Jane", DateTime.UtcNow);

        Assert.NotEqual(Guid.Empty, evt.EventId);
        Assert.InRange(evt.OccurredAt, before, DateTime.UtcNow);
    }

    [Fact]
    public void IntegrationEvent_KeepsItsIdAndTimestamp_ThroughMassTransitSerialization()
    {
        var sent = new CustomerRegisteredIntegrationEvent(Guid.NewGuid(), null, null, DateTime.UtcNow);

        var json = JsonSerializer.Serialize(sent, SystemTextJsonMessageSerializer.Options);
        var received = JsonSerializer.Deserialize<CustomerRegisteredIntegrationEvent>(json, SystemTextJsonMessageSerializer.Options)!;

        Assert.Equal(sent.EventId, received.EventId);
        Assert.Equal(sent.OccurredAt, received.OccurredAt);
        Assert.Equal(sent, received);
    }
}
