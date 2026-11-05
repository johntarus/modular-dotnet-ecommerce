using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Cart.Infrastructure.Persistence;

internal class CartDbContextFactory : IDesignTimeDbContextFactory<CartDbContext>
{
    public CartDbContext CreateDbContext(string[] args)
    {
        var optionBuilder = new DbContextOptionsBuilder<CartDbContext>();
        optionBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=modular_monolith;Username=postgres;Password=postgres");

        // For design-time only (migrations), use a null publisher
        return new CartDbContext(optionBuilder.Options, new NoOpPublisher());
    }
}

// Design-time publisher that does nothing
internal class NoOpPublisher : IPublisher
{
    public Task Publish(object notification, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
        => Task.CompletedTask;
}