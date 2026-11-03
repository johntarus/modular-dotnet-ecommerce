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
        modelBuilder.Entity<ShoppingCart>(e =>
        {
            e.ToTable("Carts", t =>
                t.HasCheckConstraint(
                    "CK_Cart_Identity",
                    "(\"UserId\" IS NOT NULL AND \"GuestTokenHash\" IS NULL) OR (\"UserId\" IS NULL AND \"GuestTokenHash\" IS NOT NULL)"));

            e.HasIndex(c => c.UserId)
                .IsUnique()
                .HasFilter("\"UserId\" IS NOT NULL");   // ← partial index

            e.HasIndex(c => c.GuestTokenHash)
                .IsUnique()
                .HasFilter("\"GuestTokenHash\" IS NOT NULL");
        });

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }

    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
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