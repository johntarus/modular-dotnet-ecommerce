using BuildingBlocks.Domain;
using Cart.Domain.Entities;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Cart.Infrastructure.Persistence;

internal class CartDbContext(
    DbContextOptions<CartDbContext> options,
    IPublisher publisher) : DbContext(options)
{
    public DbSet<ShoppingCart> Carts => Set<ShoppingCart>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("cart");
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CartDbContext).Assembly);

        // MassTransit Inbox/Outbox Tables
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entities = ChangeTracker
            .Entries<BaseEntity>()
            .Where(x => x.Entity.DomainEvents.Count != 0)
            .Select(x => x.Entity)
            .ToList();

        var domainEvents = entities
            .SelectMany(x => x.DomainEvents)
            .ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var domainEvent in domainEvents)
        {
            await publisher.Publish(domainEvent, cancellationToken);
        }

        entities.ForEach(x => x.ClearDomainEvents());

        return result;
    }
}