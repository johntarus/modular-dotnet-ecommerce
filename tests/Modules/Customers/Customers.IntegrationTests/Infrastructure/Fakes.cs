using BuildingBlocks.Infrastructure.Auth.CurrentUser;
using MediatR;

namespace Customers.IntegrationTests.Infrastructure;

public sealed class FakeCurrentUser : ICurrentUser
{
    public Guid? UserId { get; set; }
    public bool IsAuthenticated { get; set; }

    public void SignIn(Guid userId)
    {
        UserId = userId;
        IsAuthenticated = true;
    }

    public void SignOut()
    {
        UserId = null;
        IsAuthenticated = false;
    }
}

/// <summary>Captures what CustomersDbContext dispatches after SaveChanges.</summary>
public sealed class RecordingPublisher : IPublisher
{
    public List<object> Published { get; } = [];

    public Task Publish(object notification, CancellationToken cancellationToken = default)
    {
        Published.Add(notification);
        return Task.CompletedTask;
    }

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        Published.Add(notification!);
        return Task.CompletedTask;
    }
}
