using BuildingBlocks.Domain;
using BuildingBlocks.Shared;
using Catalog.Domain.Entities;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence;

internal class CatalogDbContext(DbContextOptions<CatalogDbContext> options, IPublisher publisher) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("catalog");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
        
        // Enable the Postgres extension
        modelBuilder.HasPostgresExtension("vector");

        // Map the Vector property to the pgvector column type
        modelBuilder.Entity<Product>()
            .Property(p => p.Embedding)
            .HasColumnType("vector(1536)"); // 1536 = text-embedding-3-small dimension

        // Optional: Add an index for faster cosine distance searches
        modelBuilder.Entity<Product>()
            .HasIndex(p => p.Embedding)
            .HasMethod("ivfflat") // or "hnsw" on pgvector 0.5+
            .HasOperators("vector_cosine_ops");

    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Dispatch domain events before saving / use an outbox pattern to avoid synchronous operations here
        await DispatchDomainEventsAsync(cancellationToken);

        return await base.SaveChangesAsync(cancellationToken);
    }

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        var domainEntities = ChangeTracker
            .Entries<BaseEntity>()
            .Where(x => x.Entity.DomainEvents.Any())
            .Select(x => x.Entity)
            .ToList();

        var domainEvents = domainEntities
            .SelectMany(x => x.DomainEvents)
            .ToList();

        domainEntities.ForEach(entity => entity.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
        {
            await publisher.Publish(domainEvent, cancellationToken);
        }
    }
}