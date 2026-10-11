using MediatR;

namespace BuildingBlocks.Contracts.Events;

public interface IIntegrationEvent : INotification
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
}
