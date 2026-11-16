using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Notifications.Infrastructure.Persistence;

internal sealed class NotificationsDbContextFactory
    : IDesignTimeDbContextFactory<NotificationsDbContext>
{
    public NotificationsDbContext CreateDbContext(string[] args)
    {
        var optionBuilder = new DbContextOptionsBuilder<NotificationsDbContext>();
        optionBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=modular_monolith;Username=postgres;Password=postgres");

        // For design-time only (migrations), use a null publisher
        return new NotificationsDbContext(optionBuilder.Options, new NoOpPublisher());
    }
}

internal class NoOpPublisher : IPublisher
{
    public Task Publish(object notification, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
        => Task.CompletedTask;
}