using Catalog.Domain.Entities;
using Catalog.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Catalog.IntegrationTests.Infrastructure;

/// <summary>One Postgres container for the whole run, with the real Catalog migrations applied.</summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder().WithImage("postgres:17").Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var context = CreateContext(new NoOpPublisher());
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    internal CatalogDbContext CreateContext(IPublisher publisher) =>
        new(new DbContextOptionsBuilder<CatalogDbContext>().UseNpgsql(ConnectionString).Options, publisher);

    public async Task ResetAsync()
    {
        await using var context = CreateContext(new NoOpPublisher());
        await context.Database.ExecuteSqlRawAsync("""TRUNCATE "catalog"."products", "catalog"."categories";""");
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Postgres";
}

/// <summary>Captures the domain events CatalogDbContext dispatches on save.</summary>
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

[Collection(PostgresCollection.Name)]
public abstract class CatalogTestBase(PostgresFixture db) : IAsyncLifetime
{
    protected PostgresFixture Db { get; } = db;
    protected RecordingPublisher DomainEvents { get; } = new();

    public Task InitializeAsync() => Db.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>A context whose dispatched domain events are recorded in <see cref="DomainEvents"/>.</summary>
    internal CatalogDbContext NewContext() => Db.CreateContext(DomainEvents);

    protected async Task<Guid> SeedCategoryAsync(string name = "Kitchen", bool deleted = false)
    {
        var category = Category.Create(name, null);
        if (deleted) category.Delete();
        await using var context = Db.CreateContext(new NoOpPublisher());
        context.Categories.Add(category);
        await context.SaveChangesAsync();
        return category.Id;
    }

    protected async Task<Guid> SeedProductAsync(Guid categoryId, string name = "Mug", decimal price = 10m, bool deleted = false)
    {
        var product = Product.Create(name, "Description", price, categoryId);
        if (deleted) product.Delete();
        await using var context = Db.CreateContext(new NoOpPublisher());
        context.Products.Add(product);
        await context.SaveChangesAsync();
        return product.Id;
    }

    /// <summary>Reads straight from the database, including soft-deleted rows.</summary>
    protected async Task<Product?> LoadProductAsync(Guid id)
    {
        await using var context = Db.CreateContext(new NoOpPublisher());
        return await context.Products.AsNoTracking().IgnoreQueryFilters().SingleOrDefaultAsync(p => p.Id == id);
    }

    protected async Task<Category?> LoadCategoryAsync(Guid id)
    {
        await using var context = Db.CreateContext(new NoOpPublisher());
        return await context.Categories.AsNoTracking().IgnoreQueryFilters().SingleOrDefaultAsync(c => c.Id == id);
    }
}
