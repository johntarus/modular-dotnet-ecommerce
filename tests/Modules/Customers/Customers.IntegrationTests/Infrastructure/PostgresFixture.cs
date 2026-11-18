using Customers.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Customers.IntegrationTests.Infrastructure;

/// <summary>
/// One Postgres container for the whole test run, with the real Customers migrations applied.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:17")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    internal CustomersDbContext CreateContext(IPublisher? publisher = null) =>
        new(new DbContextOptionsBuilder<CustomersDbContext>().UseNpgsql(ConnectionString).Options,
            publisher ?? new RecordingPublisher());

    public async Task ResetAsync()
    {
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync("""TRUNCATE "customers"."Customers" CASCADE;""");
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Postgres";
}
